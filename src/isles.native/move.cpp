#include "move.h"
#include <vector>

b2World* move_new()
{
	return new b2World({});
}

void move_delete(b2World* world)
{
	delete world;
}

struct OverlapQuery : b2QueryCallback
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

struct SnapToContactRayCast : b2RayCastCallback
{
	b2Body* body;
	float minFraction;

	float ReportFixture(b2Fixture* fixture, const b2Vec2& point, const b2Vec2& normal, float fraction) override
	{
		if (fixture == body->GetFixtureList())
			return -1;

		if (fraction < minFraction)
			minFraction = fraction;
		return fraction;
	}
};

static void snap_to_contact(b2World* world, b2Body* body, float radius, const b2Vec2& center)
{
	auto pos = body->GetPosition();

	SnapToContactRayCast rayCast;
	rayCast.body = body;

	if (abs(pos.y - center.y) > radius) {
		rayCast.minFraction = FLT_MAX;
		world->RayCast(&rayCast, pos, {pos.x, center.y});
		auto f = rayCast.minFraction;
		f = f == FLT_MAX ? 1.0f : f - radius / abs(center.y - pos.y);
		if (f > 0)
			pos.y += f * (center.y - pos.y);
	}

	if (abs(pos.x - center.x) > radius) {
		rayCast.minFraction = FLT_MAX;
		world->RayCast(&rayCast, pos, {center.x, pos.y});
		auto f = rayCast.minFraction;
		f = f == FLT_MAX ? 1.0f :
			f - radius / abs(center.x - pos.x);
		if (f > 0)
			pos.x += f * (center.x - pos.x);
	}

	body->SetTransform(pos, 0);
}

static void update_spawn_position(b2World* world, b2Body* body, float radius)
{
	const int MaxSpawnSearchSteps = 1000;

	OverlapQuery query;
	query.fixtureB = body->GetFixtureList();

	float r = 0, a = 0;

	b2Vec2 center = body->GetPosition();
	for (int i = 0; i < MaxSpawnSearchSteps; i++) {
		query.manifold.pointCount = 0;
		world->QueryAABB(&query, body->GetFixtureList()->GetAABB(0));
		if (query.manifold.pointCount == 0) {
			if (i != 0)
				snap_to_contact(world, body, radius, center);
			return;
		}

		if (r > 0)
			a += 2 * asinf(radius / r);
		if (a < FLT_EPSILON)
			r += radius * 2;
		else if (a > b2_pi * 2) {
			a = 0;
			r += radius * 2;
		}

		b2Vec2 position{center.x + r * cosf(a), center.y + r * sinf(a)};
		body->SetTransform(position, 0);
	}

	// Give up
	body->SetTransform(center, 0);
}

static b2Body* create_movable(b2World* world, const Movable& m) {
	b2CircleShape shape;
	shape.m_radius = m.radius;

	b2BodyDef bd;
	bd.enabled = true;
	bd.fixedRotation = true;
	bd.type = b2_dynamicBody;
	bd.position = m.position;

	b2FixtureDef fd;
	fd.shape = &shape;
	fd.friction = 0;
	fd.restitutionThreshold = FLT_MAX;
	fd.density = 1.0f / (b2_pi * m.radius * m.radius);

	auto body = world->CreateBody(&bd);
	body->CreateFixture(&fd);

	update_spawn_position(world, body, m.radius * 1.01f);
	return body;
}

static b2Body* create_obstacle(b2World* world, const Obstacle& obstacle)
{
	b2BodyDef bd;
	bd.enabled = true;
	bd.type = b2_staticBody;
	bd.position = obstacle.position;

	b2PolygonShape polygon;
	polygon.SetAsBox(obstacle.size / 2, obstacle.size / 2);

	b2FixtureDef fd;
	fd.density = 0;
	fd.friction = 0;
	fd.restitutionThreshold = FLT_MAX;
	fd.shape = &polygon;

	auto body = world->CreateBody(&bd);
	body->CreateFixture(&fd);
	return body;
}

static inline bool is_movable(b2Body* body)
{
	return body->GetType() == b2_dynamicBody;
}

static inline bool is_obstacle(b2Body* body)
{
	return body->GetType() == b2_staticBody;
}

void sync_state_before_step(
	b2World* world, Movable* movables, int32_t movablesLength,
	Obstacle* obstacles, int32_t obstaclesLength)
{
	for (auto body = world->GetBodyList(); body; body = body->GetNext())
		body->GetUserData().pointer = -1;

	// Upsert movables
	for (int i = 0; i < movablesLength; i++) {
		auto& m = movables[i];
		if (m.body == nullptr)
			m.body = create_movable(world, m);
		m.body->GetUserData().pointer = i;
		if (m.force.LengthSquared() > b2_epsilon * b2_epsilon)
			m.body->ApplyForceToCenter(m.force, m.flags & kMovable_Wake);
		m.flags = 0;
	}

	// Upsert obstacles
	for (int i = 0; i < obstaclesLength; i++) {
		auto& obstacle = obstacles[i];
		if (obstacle.body == nullptr)
			obstacle.body = create_obstacle(world, obstacle);
		obstacle.body->GetUserData().pointer = i;
	}

	// Delete unreferenced bodies
	for (auto body = world->GetBodyList(); body;) {
		auto next = body->GetNext();
		if (body->GetUserData().pointer == -1)
			world->DestroyBody(body);
		body = next;
	}
}

void sync_state_after_step(b2World* world, Movable* movables, int32_t movablesLength)
{
	for (auto contact = world->GetContactList(); contact; contact = contact->GetNext()) {
		auto a = contact->GetFixtureA()->GetBody();
		auto b = contact->GetFixtureB()->GetBody();
		auto flag = kMovable_HasContact;
		if (contact->IsTouching())
			flag |= kMovable_HasTouchingContact; 
		if (is_movable(a))
			movables[a->GetUserData().pointer].flags |= flag;
		if (is_movable(b))
			movables[b->GetUserData().pointer].flags |= flag;
	}

	for (int i = 0; i < movablesLength; i++) {
		auto& m = movables[i];
		m.position = m.body->GetPosition();
		m.velocity = m.body->GetLinearVelocity();
		if (m.body->IsAwake())
			m.flags |= kMovable_Awake;
	}
}

void move_step(
	b2World* world, float dt, Movable* movables, int32_t movablesLength,
	Obstacle* obstacles, int32_t obstaclesLength)
{
	sync_state_before_step(world, movables, movablesLength, obstacles, obstaclesLength);

	world->Step(dt, 8, 3);

	sync_state_after_step(world, movables, movablesLength);
}

int32_t move_get_next_contact(b2World* world, void** iterator, MoveContact* contact)
{
	assert(iterator != nullptr);

	auto current = *iterator == nullptr
		? world->GetContactList()
		: reinterpret_cast<b2Contact*>(*iterator)->GetNext();

	for (; current; current = current->GetNext()) {
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
	}

	return 0;
}
