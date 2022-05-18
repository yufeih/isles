#pragma once

#include <cstdint>
#include <box2d/box2d.h>

#ifdef _WIN32
#define EXPORT_API extern "C" __declspec(dllexport)
#else
#define EXPORT_API extern "C"
#endif

struct MoveUnit
{
    int32_t id;
    float radius;
    b2Vec2 position;
    b2Vec2 force;
};

struct MoveObstacle
{
    int32_t id;
    b2Vec2* vertices;
    int32_t length;
    b2Vec2 position;
};

struct MoveContact
{
    int32_t a;
    int32_t b;
};

EXPORT_API b2World* move_new();
EXPORT_API void move_delete(b2World* world);
EXPORT_API void move_step(b2World* world, float dt);
EXPORT_API b2Body* move_set_unit(b2World* world, b2Body* body, MoveUnit* unit);
EXPORT_API b2Body* move_set_obstacle(b2World* world, b2Body* body, MoveObstacle* obstacle);
EXPORT_API void move_get_unit(b2Body* unit, b2Vec2* position, b2Vec2* velocity);
EXPORT_API int32_t move_get_next_contact(b2World* world, void** iterator, MoveContact* contact);
