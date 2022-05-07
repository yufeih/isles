#pragma once

#include <cstdint>
#include <box2d/b2_math.h>

#ifdef _WIN32
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C"
#endif

struct MoveUnit
{
    float radius;
    b2Vec2 position;
    b2Vec2 velocity;
    b2Vec2 force;
};

struct MoveObstacle
{
    int32_t (*get_polygon)(const MoveObstacle*, b2Vec2*);
    b2Vec2 position;
};

struct MoveContact
{
    int32_t a;
    int32_t b;
};

struct MoveWorld;

EXPORT_API MoveWorld* move_new();
EXPORT_API void move_delete(MoveWorld* world);
EXPORT_API void move_step(MoveWorld* world, float dt,
    MoveUnit* units, int32_t unitsLength, MoveObstacle* obstacles, int32_t obstaclesLength);
EXPORT_API int32_t move_get_next_contact(MoveWorld* world, void** iterator, MoveContact* contact);
