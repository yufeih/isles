#pragma once

#include "api.h"
#include <box2d/box2d.h>

typedef b2World* move_world;
typedef b2Body* move_unit;
typedef b2Body* move_obstacle;

EXPORT_API move_world move_world_new();
EXPORT_API void move_world_delete(move_world world);
EXPORT_API void move_world_step(move_world world, float timeStep);

EXPORT_API move_unit move_unit_add(move_world world, float radius, float x, float y);
EXPORT_API void move_unit_remove(move_world world, move_unit unit);
EXPORT_API int32_t move_unit_is_awake(move_unit unit);
EXPORT_API void move_unit_get(move_unit unit, float* x, float* y, float* vx, float* vy);
EXPORT_API void move_unit_set_velocity(move_unit unit, float x, float y);
EXPORT_API void move_unit_apply_force(move_unit unit, float x, float y);
EXPORT_API void move_unit_apply_impulse(move_unit unit, float x, float y);

EXPORT_API move_obstacle move_obstacle_add(move_world world, float x, float y, float w, float h);
EXPORT_API void move_obstacle_remove(move_world world, move_obstacle obstacle);
