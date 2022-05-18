#include "api.h"
#include <vector>
#include <box2d/box2d.h>
#include <iostream>

b2World* move_new()
{
	return new b2World({});
}

void move_delete(b2World* world)
{
	delete world;
}

struct UnitOverlapQuery : b2QueryCallback
{
	b2Fixture* fixtureB;
	b2Manifold manifold;

	bool ReportFixture(b2Fixture* fixtureA) override
	{
		if (fixtureA == fixtureB)
			return true;

		switch (fixtureA->GetShape()->GetType()) {
		case b2Shape::e_circle:
			b2CollideCircles(
				&manifold,
				reinterpret_cast<b2CircleShape*>(fixtureA->GetShape()),
				fixtureA->GetBody()->GetTransform(),
				reinterpret_cast<b2CircleShape*>(fixtureB->GetShape()),
				fixtureB->GetBody()->GetTransform());
			break;
		case b2Shape::e_polygon:
			b2CollidePolygonAndCircle(
				&manifold,
				reinterpret_cast<b2PolygonShape*>(fixtureA->GetShape()),
				fixtureA->GetBody()->GetTransform(),
				reinterpret_cast<b2CircleShape*>(fixtureB->GetShape()),
				fixtureB->GetBody()->GetTransform());
			break;
		default:
			return true;
		}

		return manifold.pointCount == 0;
	}
};

static void update_unit_spawn_position(b2World* world, b2Body* body, float radius)
{
	const int MaxSpawnSearchSteps = 1000;
	UnitOverlapQuery query;
	query.fixtureB = body->GetFixtureList();

	b2Vec2 center = body->GetPosition();
	float r = 0, a = 0;

	for (int i = 0; i < MaxSpawnSearchSteps; i++) {
		std::cout << body->GetPosition().x << " " << body->GetPosition().y;
		query.manifold.pointCount = 0;
		world->QueryAABB(&query, body->GetFixtureList()->GetAABB(0));
		if (query.manifold.pointCount == 0)
			return;

		if (r > 0)
			a += 2 * asinf(radius / r);
		if (a < FLT_EPSILON)
			r += radius * 2;
		else if (a > M_PI * 2) {
			a = 0;
			r += radius * 2;
		}

		b2Vec2 position{center.x + r * cosf(a), center.y + r * sinf(a)};
		body->SetTransform(position, 0);
	}

	// Give up
	body->SetTransform(center, 0);
}

b2Body* move_set_unit(b2World* world, b2Body* body, MoveUnit* unit)
{
	if (body == nullptr) {
		b2CircleShape shape;
		shape.m_radius = unit->radius;

		b2BodyDef bd;
		bd.enabled = true;
		bd.fixedRotation = true;
		bd.type = b2_dynamicBody;
		bd.position = unit->position;

		b2FixtureDef fd;
		fd.shape = &shape;
		fd.friction = 0;
		fd.restitutionThreshold = FLT_MAX;
		fd.density = 1.0f / (b2_pi * unit->radius * unit->radius);

		body = world->CreateBody(&bd);
		body->CreateFixture(&fd);

		update_unit_spawn_position(world, body, unit->radius * 1.01f);
	}

	if (unit->id < 0) {
		world->DestroyBody(body);
		return nullptr;
	}

	body->GetUserData().pointer = unit->id;
	body->ApplyForceToCenter(unit->force, true);
	return body;
}

b2Body* move_set_obstacle(b2World* world, b2Body* body, MoveObstacle* obstacle)
{
	if (body == nullptr) {
		b2BodyDef bd;
		bd.enabled = true;
		bd.type = b2_staticBody;
		bd.position = obstacle->position;

		b2FixtureDef fd;
		fd.density = 0;
		fd.friction = 0;
		fd.restitutionThreshold = FLT_MAX;

		b2PolygonShape polygon;
		b2ChainShape chain;

		if (obstacle->length <= b2_maxPolygonVertices) {
			polygon.Set(obstacle->vertices, obstacle->length);
			fd.shape = &polygon;
		} else {
			chain.CreateLoop(obstacle->vertices, obstacle->length);
			fd.shape = &chain;
		}
		body = world->CreateBody(&bd);
		body->CreateFixture(&fd);
	}

	if (obstacle->id < 0) {
		world->DestroyBody(body);
		return nullptr;
	}

	body->GetUserData().pointer = obstacle->id;
	return body;
}

void move_get_unit(b2Body* unit, b2Vec2* position, b2Vec2* velocity)
{
	*position = unit->GetPosition();
	*velocity = unit->GetLinearVelocity();
}

void move_step(b2World* world, float dt)
{
	world->Step(dt, 8, 3);
}

int32_t move_get_next_contact(b2World* world, void** iterator, MoveContact* contact)
{
	assert(iterator != nullptr);

	auto current = *iterator == nullptr
		? world->GetContactList()
		: reinterpret_cast<b2Contact*>(*iterator)->GetNext();

	while (current != nullptr)
	{
		if (current->IsEnabled() && current->IsTouching()) {
			auto a = current->GetFixtureA()->GetBody();
			auto b = current->GetFixtureB()->GetBody();

			if (a->GetType() == b2_dynamicBody && a->IsAwake() &&
				b->GetType() == b2_dynamicBody && b->IsAwake()) {

				contact->a = a->GetUserData().pointer;
				contact->b = b->GetUserData().pointer;
				*iterator = current;
				return 1;
			}
		}
		current = current->GetNext();
	}

	return 0;
}

struct QueryAABB : b2QueryCallback
{
	int32_t* begin;
	int32_t* end;

	bool ReportFixture(b2Fixture* fixture) override
	{
		auto body = fixture->GetBody();
		if (body->GetType() == b2_dynamicBody)
			*begin++ = body->GetUserData().pointer;
		return begin != end;
	}
};

int32_t move_query_aabb(b2World* world, const b2Vec2* min, const b2Vec2* max, int32_t* result, int32_t length)
{
	QueryAABB query;
	query.begin = result;
	query.end = result + length;

	b2AABB aabb{*min, *max};
	world->QueryAABB(&query, aabb);
	return query.begin - result;
}
