#pragma once

#include "api.h"

struct MoveUnit
{
    float radius;
    float speed; // settable
    float x, y; // settable
    float vx, vy;
};

struct MoveWorld;

EXPORT_API MoveWorld* move_new();
EXPORT_API void move_delete(MoveWorld* world);
EXPORT_API void move_step(MoveWorld* world, MoveUnit *units, int unitLength, float timeStep);
