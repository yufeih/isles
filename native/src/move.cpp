#include <move.h>

move_world move_world_new()
{
  return new b2World({});
}

void move_world_delete(move_world world)
{
  delete world;
}

void move_world_step(move_world world, float timeStep)
{
  world->Step(timeStep, 8, 3);
}

move_unit move_unit_add(move_world world, float radius, float x, float y)
{
  b2CircleShape shape;
  shape.m_radius = radius;

  b2BodyDef bd;
  bd.fixedRotation = true;
  bd.type = b2_dynamicBody;
  bd.position.Set(x, y);

  b2FixtureDef fd = {};
  fd.shape = &shape;

  b2MassData mass = {1, b2Vec2_zero};

  auto body = world->CreateBody(&bd);
  body->CreateFixture(&fd);
  body->SetMassData(&mass);
  return body;
}

void move_unit_remove(move_world world, move_unit unit)
{
  world->DestroyBody(unit);
}

int32_t move_unit_is_awake(move_unit unit)
{
  return unit->IsAwake() ? 1 : 0;
}

void move_unit_get(move_unit unit, float *x, float *y, float *vx, float *vy)
{
  auto pos = unit->GetPosition();
  *x = pos.x;
  *y = pos.y;

  auto vel = unit->GetLinearVelocity();
  *vx = vel.x;
  *vy = vel.y;
}

void move_unit_set_velocity(move_unit unit, float x, float y)
{
  b2Vec2 v{x, y};
  unit->SetLinearVelocity(v);
}

void move_unit_apply_force(move_unit unit, float x, float y)
{
  b2Vec2 f{x, y};
  unit->ApplyForceToCenter(f, true);
}

void move_unit_apply_impulse(move_unit unit, float x, float y)
{
  b2Vec2 i{x, y};
  unit->ApplyLinearImpulseToCenter(i, true);
}

move_obstacle move_obstacle_add(move_world world, float x, float y, float w, float h)
{
  auto hx = w * 0.5f;
  auto hy = w * 0.5f;

  b2PolygonShape shape;
  shape.SetAsBox(hx, hy);

  b2BodyDef bd;
  bd.type = b2_staticBody;
  bd.position.Set(x + hx, y + hy);

  auto body = world->CreateBody(&bd);
  body->CreateFixture(&shape, 0.0f);
  return body;
}

void move_obstacle_remove(move_world world, move_obstacle obstacle)
{
  world->DestroyBody(obstacle);
}
