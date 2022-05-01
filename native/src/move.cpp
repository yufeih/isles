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
		if (contact->IsEnabled() &&
			contact->GetFixtureA()->GetBody()->IsAwake() &&
			contact->GetFixtureB()->GetBody()->IsAwake()) {

			if (contacts != nullptr && count < contactsLength) {
				MoveContact c;
				b2WorldManifold manifold;
				contact->GetWorldManifold(&manifold);
				c.a = contact->GetFixtureA()->GetUserData().pointer;
				c.b = contact->GetFixtureB()->GetUserData().pointer;
				c.normal = manifold.normal;
				*contacts++ = c;
			}
			count++;
		}
		contact = contact->GetNext();
	}
	return count;
}

struct MoveQueryCallback : b2QueryCallback
{
	int32_t* begin;
	int32_t* end;

	virtual bool ReportFixture(b2Fixture* fixture)
	{
		if (begin == end)
			return false;

		*begin++ = fixture->GetUserData().pointer;
		return true;
	}
};

int32_t move_query_aabb(MoveWorld* world, b2AABB* aabb, int32_t* units, int32_t unitsLength)
{
	MoveQueryCallback cb;
	cb.begin = units;
	cb.end = units + unitsLength;

	world->b2.QueryAABB(&cb, *aabb);
	return cb.end - cb.begin;
}

struct MoveRayCastCallback : b2RayCastCallback
{
	int32_t* unit;
	int32_t result;

	virtual float ReportFixture(b2Fixture* fixture, const b2Vec2& point, const b2Vec2& normal, float fraction)
	{
		*unit = fixture->GetUserData().pointer;
		result = 1;
		return 0;
	}
};

int32_t move_raycast(MoveWorld* world, b2Vec2* a, b2Vec2* b, int32_t* unit)
{
	MoveRayCastCallback cb;
	cb.result = 0;
	cb.unit = unit;

	world->b2.RayCast(&cb, *a, *b);
	return cb.result;
}
