#include "api.h"
#include <mapbox/earcut.hpp>

namespace mapbox {
namespace util {

template <>
struct nth<0, b2Vec2> {
    inline static auto get(const b2Vec2 &t) {
        return t.x;
    };
};
template <>
struct nth<1, b2Vec2> {
    inline static auto get(const b2Vec2 &t) {
        return t.y;
    };
};

} // namespace util
} // namespace mapbox

struct NavMeshPolygon
{
    std::vector<std::vector<b2Vec2>> polygon;
    std::vector<uint16_t> triangles;
};

EXPORT_API NavMeshPolygon* navmesh_new_polygon()
{
    return new NavMeshPolygon();
}

EXPORT_API void navmesh_delete_polygon(NavMeshPolygon* polygon)
{
    delete polygon;
}

EXPORT_API void navmesh_polygon_add_polylines(NavMeshPolygon* polygon, b2Vec2* vertices, int length)
{
    std::vector<b2Vec2> polylines;
    polylines.assign(vertices, vertices + length);
    polygon->polygon.push_back(std::move(polylines));
}

EXPORT_API int32_t navmesh_polygon_triangulate(NavMeshPolygon* polygon, uint16_t** indices)
{
    polygon->triangles = mapbox::earcut<uint16_t>(polygon->polygon);
    *indices = polygon->triangles.data();
    return polygon->triangles.size();
}
