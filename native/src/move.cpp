#include <move.h>
#include <box2d/b2_world.h>

static b2Vec2 zero;

struct move_world
{
  b2World _;

  move_world() : _(b2World({})) { }
};

struct move_unit
{

};

struct move_obstacle
{

};

move_world* move_world_new()
{
  return new move_world();
}

void move_world_delete(move_world* world)
{
  delete world;
}

void move_world_step(move_world* world)
{
  world->_.Step(1.0f / 60, 8, 3);
}

move_unit* move_add_unit(move_world* world, float x, float y, float radius)
{
  return nullptr;
}

void move_remove_unit(move_world* world, move_unit* unit)
{

}

void move_get_unit(move_world* world, move_unit* unit, float* x, float* y, float* vx, float* vy)
{

}


move_obstacle* move_add_obstacle(move_world* world, float x, float y, float w, float h)
{
  return nullptr;
}

void move_remove_obstacle(move_world* world, move_obstacle* unit)
{

}

