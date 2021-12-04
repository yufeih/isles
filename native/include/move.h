#pragma once

#include "api.h"
#include <box2d/b2_world.h>

typedef b2World* move_world;
typedef b2Body* move_unit;
typedef b2Fixture* move_obstacle;

EXPORT_API move_world move_world_new();
EXPORT_API void move_world_delete(move_world world);
EXPORT_API void move_world_step(move_world world);

EXPORT_API move_unit move_add_unit(move_world world, float x, float y, float radius);
EXPORT_API void move_remove_unit(move_world world, move_unit unit);
EXPORT_API void move_get_unit(move_world world, move_unit unit, float* x, float* y, float* vx, float* vy);

EXPORT_API move_obstacle move_add_obstacle(move_world world, float x, float y, float w, float h);
EXPORT_API void move_remove_obstacle(move_world world, move_obstacle unit);
