#include <move.h>
#include <vector>

struct MoveWorld
{
	b2World b2;
	std::vector<b2Body*> bodies;

	MoveWorld() : b2({}) {}
};

MoveWorld* move_new()
{
	return new MoveWorld;
}

void move_delete(MoveWorld* world)
{
	delete world;
}

MoveUnit& get_unit(void* units, int unitSizeInBytes, int i)
{
	return *reinterpret_cast<MoveUnit*>(reinterpret_cast<std::byte*>(units) + i * unitSizeInBytes);
}

b2Body* create_body(b2World& b2, const MoveUnit& unit)
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
	body->CreateFixture(&fd);
	return body;
}

void move_step(MoveWorld* world, void* units, int unitLength, int unitSizeInBytes, float dt)
{
	auto& bodies = world->bodies;
	auto& b2 = world->b2;

	for (auto i = 0; i < unitLength; i ++) {
		auto& unit = get_unit(units, unitSizeInBytes, i);
		if (i >= bodies.size()) {
			bodies.push_back(create_body(b2, unit));
		}
		bodies[i]->ApplyForceToCenter(unit.force, unit.force.x != 0 || unit.force.y != 0);
	}

	b2.Step(dt, 8, 3);

	for (auto i = 0; i < unitLength; i++) {
		auto& unit = get_unit(units, unitSizeInBytes, i);
		unit.position = bodies[i]->GetPosition();
		unit.velocity = bodies[i]->GetLinearVelocity();
	}
}
