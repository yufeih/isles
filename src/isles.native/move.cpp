#include "api.h"
#include <vector>
#include <box2d/box2d.h>

b2World* move_new()
{
	return new b2World({});
}

void move_delete(b2World* world)
{
	delete world;
}

b2Body* move_set_unit(b2World* world, b2Body* body, MoveUnit* unit)
{
	if (body == nullptr) {
		b2CircleShape shape;
		shape.m_radius = unit->radius;

		b2BodyDef bd;
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