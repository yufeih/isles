#include <move.h>

move_world move_world_new()
{
  b2Vec2 g;
  return new b2World(g);
}

void move_world_delete(move_world world)
{
  delete world;
}

void move_world_step(move_world world, float timeStep)
{
  world->Step(timeStep, 8, 3);
}

move_unit move_add_unit(move_world world, float x, float y, float radius)
{
  b2CircleShape shape;
  shape.m_radius = radius;

  b2BodyDef bd;
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

void move_get_unit(move_unit unit, float* x, float* y, float* vx, float* vy)
{
  auto pos = unit->GetPosition();
  *x = pos.x;
  *y = pos.y;

  auto vel = unit->GetLinearVelocity();
  *vx = vel.x;
  *vy = vel.y;
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
