#include "api.h"
#include <vector>
#include <box2d/box2d.h>

struct MoveWorld
{
	b2World b2;
	std::vector<b2Body*> units;
	std::vector<b2Body*> obstacles;

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

b2Body* create_unit(b2World& b2, const MoveUnit& unit, int32_t i)
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
	body->GetUserData().pointer = i;
	return body;
}

b2Body* create_obstacle(b2World& b2, const MoveObstacle& obstacle, int32_t i)
{
	b2BodyDef bd;
	bd.type = b2_staticBody;
	bd.position = obstacle.position;
	auto body = b2.CreateBody(&bd);

	b2FixtureDef fd;
	fd.density = 0;
	fd.friction = 0;
	fd.restitutionThreshold = FLT_MAX;

	b2PolygonShape polygon;
	b2ChainShape chain;

	auto length = obstacle.get_polygon(&obstacle, nullptr);
	std::vector<b2Vec2> vertices(length);
	obstacle.get_polygon(&obstacle, vertices.data());
	if (length <= b2_maxPolygonVertices) {
		polygon.Set(vertices.data(), length);
		fd.shape = &polygon;
	} else {
		chain.CreateLoop(vertices.data(), length);
		fd.shape = &chain;
	}
	body->CreateFixture(&fd);
	body->GetUserData().pointer = i;
	return body;
}

void move_step(MoveWorld* world, float dt,
    MoveUnit* units, int32_t unitsLength, MoveObstacle* obstacles, int32_t obstaclesLength)
{
	for (auto i = 0; i < unitsLength; i ++) {
		auto& unit = units[i];
		if (i >= world->units.size()) {
			world->units.push_back(create_unit(world->b2, unit, i));
		}
		world->units[i]->ApplyForceToCenter(unit.force, unit.force.x != 0 || unit.force.y != 0);
	}

	for (auto i = 0; i < obstaclesLength; i++) {
		if (i >= world->obstacles.size()) {
			world->obstacles.push_back(create_obstacle(world->b2, obstacles[i], i));
		}
	}

	world->b2.Step(dt, 8, 3);

	for (auto i = 0; i < unitsLength; i++) {
		units[i].position = world->units[i]->GetPosition();
		units[i].velocity = world->units[i]->GetLinearVelocity();
	}
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