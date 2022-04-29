#include <move.h>
#include <box2d/box2d.h>
#include <vector>

struct MoveWord
{
	b2World world;
	std::Vector<b2Body*> bodies;
}

MoveWorld move_world_new()
{
	return new b2World({});
}

void move_world_delete(MoveWorld world)
{
	delete world;
}

void move_world_step(MoveWorld* world, MoveUnit *units, int unitLength, float timeStep);
{
	world->Step(timeStep, 8, 3);
}

move_unit move_add_unit(move_world world, float radius, float damping, float x, float y, float vx, float vy)
{
	b2CircleShape shape;
	shape.m_radius = radius;

	b2BodyDef bd;
	bd.linearDamping = damping;
	bd.linearVelocity.x = vx;
	bd.linearVelocity.y = vy;
	bd.fixedRotation = true;
	bd.type = b2_dynamicBody;
	bd.position.Set(x, y);

	auto body = world->CreateBody(&bd);
	body->CreateFixture(&shape, 1.0f);
	return body;
}

void move_remove_unit(move_world world, move_unit unit)
{
	world->DestroyBody(unit);
}

void move_get_unit(move_unit unit, float *x, float *y, float *vx, float *vy)
{
	auto pos = unit->GetPosition();
	*x = pos.x;
	*y = pos.y;

	auto vel = unit->GetLinearVelocity();
	*vx = vel.x;
	*vy = vel.y;
}

int32_t move_get_unit_is_awake(move_unit unit)
{
	return unit->IsAwake() ? 1 : 0;
}

void move_set_unit_velocity(move_unit unit, float vx, float vy)
{
	b2Vec2 v(vx, vy);
	unit->SetLinearVelocity(v);
}

move_obstacle move_add_obstacle(move_world world, float x, float y, float w, float h)
{
	b2PolygonShape shape;
	shape.SetAsBox(w, h);

	b2BodyDef bd;
	bd.fixedRotation = true;
	bd.type = b2_staticBody;
	bd.position.Set(x, y);

	auto body = world->CreateBody(&bd);
	body->CreateFixture(&shape, 0.0f);
	return body;
}
