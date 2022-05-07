#include "api.h"
#include <vector>
#include <box2d/box2d.h>

struct MoveWorld
{
	b2World b2;
	b2Body* obstacles;
	std::vector<b2Body*> bodies;

	MoveWorld() : b2({}), obstacles(nullptr) {}
};

MoveWorld* move_new()
{
	return new MoveWorld;
}

void move_delete(MoveWorld* world)
{
	delete world;
}

MoveUnit& get_unit(void* units, int32_t sizeInBytes, int32_t i)
{
	return *reinterpret_cast<MoveUnit*>(reinterpret_cast<std::byte*>(units) + i * sizeInBytes);
}

b2Body* create_body(b2World& b2, const MoveUnit& unit, size_t i)
{
	b2CircleShape shape;
	shape.m_radius = unit.radius;

	b2BodyDef bd;
	bd.fixedRotation = true;
	bd.type = b2_dynamicBody;
	bd.position = unit.position;

	b2FixtureDef fd;
	fd.shape = &shape;
	fd.friction = 0;
	fd.restitutionThreshold = FLT_MAX;
	fd.density = 1.0f / (b2_pi * unit.radius * unit.radius);

	auto body = b2.CreateBody(&bd);
	auto fixture = body->CreateFixture(&fd);
	fixture->GetUserData().pointer = i;
	return body;
}

void move_step(MoveWorld* world, float dt, void* units, int32_t length, int32_t sizeInBytes)
{
	auto& bodies = world->bodies;
	auto& b2 = world->b2;

	for (auto i = 0; i < length; i ++) {
		auto& unit = get_unit(units, sizeInBytes, i);
		if (i >= bodies.size()) {
			bodies.push_back(create_body(b2, unit, i));
		}
		bodies[i]->ApplyForceToCenter(unit.force, unit.force.x != 0 || unit.force.y != 0);
	}

	b2.Step(dt, 8, 3);

	for (auto i = 0; i < length; i++) {
		auto& unit = get_unit(units, sizeInBytes, i);
		auto body = bodies[i];
		unit.position = body->GetPosition();
		unit.velocity = body->GetLinearVelocity();
	}
}

void move_add_obstacle(MoveWorld* world, b2Vec2* vertices, int32_t length)
{
	auto body = world->obstacles;
	if (body == nullptr) {
		b2BodyDef bd;
		bd.type = b2_staticBody;
		body = world->b2.CreateBody(&bd);
	}

	b2ChainShape shape;
	b2FixtureDef fd;
	fd.density = 0;
	fd.friction = 0;
	fd.restitutionThreshold = FLT_MAX;
	fd.shape = &shape;

	shape.CreateLoop(vertices, length);
	body->CreateFixture(&fd);
}

int32_t move_get_next_contact(MoveWorld* world, void** iterator, MoveContact* contact)
{
	assert(iterator != nullptr);

	auto current = *iterator == nullptr
		? world->b2.GetContactList()
		: reinterpret_cast<b2Contact*>(*iterator)->GetNext();

	while (current != nullptr)
	{
		if (current->IsEnabled() && current->IsTouching()) {
			auto a = current->GetFixtureA()->GetBody();
			auto b = current->GetFixtureB()->GetBody();

			if (a->GetType() == b2_dynamicBody && a->IsAwake() &&
				b->GetType() == b2_dynamicBody && b->IsAwake()) {

				contact->a = current->GetFixtureA()->GetUserData().pointer;
				contact->b = current->GetFixtureB()->GetUserData().pointer;
				*iterator = current;
				return 1;
			}
		}
		current = current->GetNext();
	}

	return 0;
}