#include <move.h>
#include <vector>
#include <set>

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

MoveUnit& get_unit(void* units, int32_t unitSizeInBytes, int32_t i)
{
	return *reinterpret_cast<MoveUnit*>(reinterpret_cast<std::byte*>(units) + i * unitSizeInBytes);
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

void move_step(MoveWorld* world, void* units, int32_t unitsLength, int32_t unitSizeInBytes, float dt)
{
	auto& bodies = world->bodies;
	auto& b2 = world->b2;

	for (auto i = 0; i < unitsLength; i ++) {
		auto& unit = get_unit(units, unitSizeInBytes, i);
		if (i >= bodies.size()) {
			bodies.push_back(create_body(b2, unit, i));
		}
		bodies[i]->ApplyForceToCenter(unit.force, unit.force.x != 0 || unit.force.y != 0);
	}

	b2.Step(dt, 8, 3);

	for (auto i = 0; i < unitsLength; i++) {
		auto& unit = get_unit(units, unitSizeInBytes, i);
		auto body = bodies[i];
		unit.position = body->GetPosition();
		unit.velocity = body->GetLinearVelocity();
	}
}

int32_t move_get_contacts(MoveWorld* world, MoveContact* contacts, int32_t contactsLength)
{
	auto count = 0;
	auto contact = world->b2.GetContactList();
	while (contact != nullptr)
	{
		if (contact->IsEnabled() && contact->IsTouching() &&
			contact->GetFixtureA()->GetBody()->IsAwake() &&
			contact->GetFixtureB()->GetBody()->IsAwake()) {

			if (contacts != nullptr && count < contactsLength) {
				MoveContact c;
				c.a = contact->GetFixtureA()->GetUserData().pointer;
				c.b = contact->GetFixtureB()->GetUserData().pointer;
				*contacts++ = c;
			}
			count++;
		}
		contact = contact->GetNext();
	}
	return count;
}
