#include <move.h>
#include <box2d/b2_world.h>

move_world move_world_new()
{
  return new b2World({});
}

void move_world_delete(move_world world)
{
  delete world;
}

void move_world_step(move_world world)
{
  world->Step(1.0f / 60, 8, 3);
}

move_unit move_add_unit(move_world world, float x, float y, float radius)
{
  return nullptr;
}

void move_remove_unit(move_world world, move_unit unit)
{

}

void move_get_unit(move_world world, move_unit unit, float* x, float* y, float* vx, float* vy)
{

}

move_obstacle move_add_obstacle(move_world world, float x, float y, float w, float h)
{
  return nullptr;
}

void move_remove_obstacle(move_world world, move_obstacle unit)
{

}

