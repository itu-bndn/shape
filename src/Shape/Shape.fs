/// Copyright (C) 2015 The Authors.
module Shape

open System // for Math

open Material
open Point
open Ray
open Texture
open Vector

[<Literal>]
let EPSILON = 1.0e-6

type Composition =
    | Union
    | Subtraction
    | Intersection

type Shape =
    | Plane of Point * Vector * Texture
    | Sphere of Point * float * Texture
    | Triangle of Point * Point * Point * Texture
    | Composite of Shape * Shape * Composition

type Hitpoint =
    | Hit of float * Vector * Material

exception NonPositiveShapeSizeException

/// <summary>
/// Get the distance, the normal and the material of the hit.
/// </summary>
/// <param name=hp>The hitpoint to get the values of.</param>
/// <returns>
/// A triplet of the distance, the normal and the material
/// of the hit.
/// </returns>
let getHitpoint (Hit(d, n, m)) = (d, n, m)

/// <summary>
/// Get the distance from the ray's origin, to the hitpoint on the shape.
/// </summary>
/// <param name=hp>The hitpoint on the shape to get the distance of.</param>
/// <returns>The distance to the hitpoint.</returns>
let getHitDistance (Hit(d, _, _)) = d

/// <summary>
/// Get the normal of the shape at the hitpoint.
/// </summary>
/// <param name=hp>The hitpoint on the shape to get the normal of.</param>
/// <returns>The normal of the shape at the hitpoint.</returns>
let getHitNormal (Hit(_, n, _)) = n

/// <summary>
/// Get the material of the shape at the hitpoint.
/// </summary>
/// <param name=hp>The hitpoint on the shape to get the material of.</param>
/// <returns>The material on the shape at the hitpoint.</returns>
let getHitMaterial (Hit(_, _, m)) = m

/// <summary>
/// Make a plane with a point of origin (affects the texture mapping),
/// an upvector and a texture.
/// </summary>
/// <param name=p0>
/// The point of origin (affects the texture mapping of the plane).
/// </param>
/// <param name=up>The upvector of the plane.</param>
/// <param name=t>The texture of the plane.</param>
/// <returns>
/// A plane object facing in the direction of the upvector, with a
/// point of origin (for texture mapping) and a texture.
/// </returns>
let mkPlane p0 up texture =
    if Vector.magnitude up = 0.
    then failwith "Attempting to create a plane with a 0-vector as the normal"
    Plane(p0, up, texture)

/// <summary>
/// Make a sphere with a point of origin, a radius and a texture.
/// </summary>
/// <param name=center>The center point of the sphere.</param>
/// <param name=radius>The radius of the sphere.</param>
/// <param name=t>The texture of the sphere.</param>
/// <returns>
/// A sphere object, with a point of origin, a radius and a texture.
/// </returns>
let mkSphere center radius texture =
    if radius <= 0. then raise NonPositiveShapeSizeException
    Sphere(center, radius, texture)

/// <summary>
/// Make a triangle with points, `a`, `b` and `c`.
/// </summary>
/// <param name=a>Point `a` in the triangle.</param>
/// <param name=b>Point `b` in the triangle.</param>
/// <param name=c>Point `c` in the triangle.</param>
/// <param name=m>The material of the triangle.</param>
/// <returns>
/// A triangle object, with points `a`, `b` and `c`, and a material.
/// </returns>
let mkTriangle a b c material =
    if a = b || a = c || b = c then raise NonPositiveShapeSizeException
    Triangle(a, b, c, Texture.make (fun x y -> material))

/// <summary>
/// Make a union between `shape1` and `shape2`.
/// </summary>
/// <param name=shape1>`shape1` is the first shape in the union.</param>
/// <param name=shape2>`shape2` is the second shape in the union.</param>
/// <returns>
/// The composite union of the two shapes, acting as a single solid shape.
/// </returns>
let mkUnion shape1 shape2 = Composite(shape1, shape2, Union)

/// <summary>
/// Subtracts `shape2` from `shape2`.
/// </summary>
/// <param name=shape1>`shape1` is the shape to be subtracted from.</param>
/// <param name=shape2>`shape2` is the shape used for subtraction.</param>
/// <returns>
/// The composite subtraction of the two shapes, acting as a single solid shape.
/// </returns>
let mkSubtraction shape1 shape2 = Composite(shape1, shape2, Subtraction)

/// <summary>
/// Make an intersection between `shape1` and `shape2`.
/// </summary>
/// <param name=shape1>`shape1` is the first shape in the intersection.</param>
/// <param name=shape2>`shape1` is the second shape in the intersection.</param>
/// <returns>
/// The composite intersection of the two shapes, acting as the overlapping areas of the two shapes.
/// </returns>
let mkIntersection shape1 shape2 = Composite(shape1, shape2, Intersection)

/// <summary>
/// Calculates the hit distances between a ray and a shape created from
/// a polynomial expression, using values a, b and c from the expression.
/// </summary>
/// <param name="a">Value of `a` in a polynomial expression.</param>
/// <param name="b">Value of `b` in a polynomial expression.</param>
/// <param name="c">Value of `c` in a polynomial expression.</param>
/// <returns>
/// The list of 0 to 2 hit distances from the ray origin to the hitpoints
/// on the shape. Only returns hitpoints that intersect in the positive
/// direction of the ray.
/// </returns>
let distance a b c =
    let D = b**2. - 4.*a*c // discriminant
    match D with
    | D when D < -EPSILON -> []
    | D when D < EPSILON  -> let d = -(b / (2.*a))
                             if d > 0. then [d] else []
    | _ -> let dneg = (-b - sqrt(D)) / (2.*a)
           let dpos = (-b + sqrt(D)) / (2.*a)
           match (dneg > 0., dpos > 0.) with
           | (true, true)   -> [dneg;dpos]
           | (true, false)  -> [dneg]
           | (false, true)  -> [dpos]
           | (false, false) -> []

/// <summary>
/// Function for sorting a hitlist into a list of triples, sorted by distance to origin of ray.
/// </summary>
/// <param name="shape1"> `shape1` is the first shape used in the composite hit.</param>
/// <param name="shape2"> `shape2` is the second shape used in the composite hit.</param>
/// <param name="hits1"> `hits1` is a list of the hitpoints on shape1.</param>
/// <param name="hits2"> `hits1` is a list of the hitpoints on shape2.</param>
/// <returns>
/// A list of triples of the type (id,shape,hit), sorted by the hits distance to the origin of the ray.
/// </returns>
let sortToTuples shape1 shape2 hits1 hits2 =
    let shape1list = List.map (fun x -> (1,shape1,x)) hits1
    let shape2list = List.map (fun x -> (2,shape2,x)) hits2
    let tupleList = shape1list @ shape2list
    List.sortWith (fun (_,_,h1) (_,_,h2) -> 
        let d1 = getHitDistance h1
        let d2 = getHitDistance h2
        if d1 > d2 then 1 elif d1 < d2 then -1 else 0) tupleList

/// <summary>
/// Function for testing if a hits normal vector is orthogonal on a ray.
/// </summary>
/// <param name="ray">`ray` is the ray responsible for the hit.</param>
/// <param name="hitNormalVector">`hitNormalVector` is the normal Vector of the hit.</param>
/// <returns>
/// A boolean value which is true if the normal vector is orthogonal, or false if it isn't.
/// </returns>
let isOrthogonal ray hitNormalVector =
    let rayVector = Ray.getVector ray
    let dp = Vector.dotProduct rayVector hitNormalVector
    dp < EPSILON && dp > -EPSILON

/// <summary>
/// A function for determining the "exit hit" in a union composite.
/// An exit hit is defined as the hit where the ray "leaves" the solid body of a shape.
/// </summary>
/// <param name="hitTupleList"> A list containing triples of the type (int,shape,hit) </param>
/// <returns>
/// A tuple containing a trimmed hitTupleList and the "exit hit".
/// </returns>
let rec findExitHit hitTupleList =
    match hitTupleList with
    | (id1,_,h) :: (id2,_,_) :: (id3,_,_) :: hitTupleList when id2 = id3 -> (hitTupleList,h)
    | (id1,_,_) :: (id2,s2,h2) :: hitTupleList when id1 <> id2 -> findExitHit ((id2,s2,h2) :: hitTupleList)
    | (id,s,h) :: hitTupleList -> (hitTupleList,h)
    | [] -> failwith "No hits in tuple list."

/// <summary>
/// Creates a hitpoint on a sphere, given a distance, a ray direction
/// a ray origin, the radius of the sphere and the texture.
/// </summary>
/// <param name=d>The distance between rayO and the sphere's surface.</param>
/// <param name=rayV>The direction of the ray (vector).</param>
/// <param name=rayO>The origin of the ray.</param>
/// <param name=r>The radius of the sphere.</param>
/// <param name=t>The texture for the sphere.</param>
/// <returns>
/// A hitpoint for the sphere, with a distance, the inverse vector
/// (going outwards from the sphere surface) and the material for the
/// hitpoint.
/// </returns>
let sphereDeterminer d center rayV rayO t =
    // hitpoint on sphere
    let hitPoint = Point.move rayO (Vector.multScalar rayV d)

    // normalised vector from hitpoint towards center
    let n = Vector.normalise <| Point.distance center hitPoint
    let (nx, ny, nz) = Vector.getCoord n

    let theta = acos ny      // angle in y-space
    let phi' = atan2 nx nz
    let phi = if phi' < 0.   // angle in x- and z-space
              then phi' + 2. * Math.PI
              else phi'

    let u = phi / (2. * Math.PI)   // u coordinate in texture space
    let v = 1. - (theta / Math.PI) // v coordinate in texture space

    let material = Texture.getMaterial u v t // material to return

    Hit(d, n, material)

/// <summary>
/// A function for checking whether a specific hitpoint was on a non-solid shape.
/// </summary>
/// <remark> This is a helper function for the shapeNonSolid function, it's calculations only necessary if a union Composite is hit.
/// <param name="point"> The point in which the hit was recorded. </param>
/// <param name="shape"> The shape hit by the ray. </param>
/// <param name="c"> The continuation. The id function at initial function call. </param>
/// <returns>
/// A boolean if the shape was non-solid, or a non-solid shape in another composite.
/// </returns>
let rec hitInNonSolid point shape c =
    match shape with
    | Plane(origin,normal,_) ->
        //Check if hitpoint is in shape.
        let pointVec = Point.distance point origin
        let dotP = (Vector.dotProduct pointVec normal)
        if dotP < EPSILON && dotP > -EPSILON
        then true else false
    | Triangle(a,b,c,_) ->
        //Calculated using Barycentric coordinates, see http://math.stackexchange.com/questions/4322/check-whether-a-point-is-within-a-3d-triangle
        let area = Vector.magnitude (Vector.crossProduct (Point.distance a b) (Point.distance a c))
        let alpha = (Vector.magnitude (Vector.crossProduct (Point.distance point b) (Point.distance point c)))/area
        if alpha < -EPSILON || alpha > (1.+EPSILON) then false
        else
            let beta = (Vector.magnitude (Vector.crossProduct (Point.distance point c) (Point.distance point a)))/area
            if beta < -EPSILON || beta > (1.+EPSILON) then false
            else
                let gamma = 1. - alpha - beta
                if gamma < -EPSILON || gamma > (1.+EPSILON) then false
                else true
    | Composite (shape1,shape2,_) ->
        hitInNonSolid point shape1 (fun s1 ->
            hitInNonSolid point shape2 (fun s2 ->
                c (s1 || s2)))
    | _ -> false


/// <summary>
/// A function for checking whether a specific hitpoint was on a non-solid shape.
/// </summary>
/// <param name="ray"> The ray responsible for the hit. </param>
/// <param name="hit"> The specific hit. </param>
/// <param name="shape"> The shape hit by the ray. </param>
/// <returns>
/// A boolean if the shape was one non-solid, or a non-solid shape in another composite.
/// </returns>
let rec shapeNonSolid ray hit shape c =
    match shape with
    | Plane(_,_,_) -> c true
    | Triangle(_,_,_,_) -> c true
    // If one of the shapes in an Intersection is 1 dimensional, both are.
    | Composite(shape1,shape2,Intersection) ->
        shapeNonSolid ray hit shape1 (fun s1 ->
            shapeNonSolid ray hit shape2 (fun s2 ->
                c (s1 || s2)))
    | Composite(shape1,shape2,Union) ->
        let rayV  = Vector.multScalar (Ray.getVector ray) (getHitDistance hit)
        let point = Point.move (Ray.getOrigin ray) rayV
        hitInNonSolid point shape (fun x -> x)
    // The difference between two shapes will always have the same solidity as shape 1
    | Composite(shape1,shape2,Subtraction) ->
        shapeNonSolid ray hit shape1 c
    | _ -> false

/// <summary>
/// Hitfunction specific to the Union composite.
/// </summary>
/// <param name="ray"> The ray to check for hits. </param>
/// <param name="hitTupleList"> A list containing triples of the type (id,shape,hit) </param>
/// <param name="hitList"> An empty list, acting as accumulator, to which the hits of the union are added. </param>
/// <returns>
/// A list of hitpoints.
/// </returns>
let rec unionHitFunction ray hitTupleList hitList =
    match hitTupleList with
    | (_,s,h) :: hitTupleList when isOrthogonal ray (getHitNormal h) || shapeNonSolid ray h s (fun x -> x) -> 
        unionHitFunction ray hitTupleList (h :: hitList)
    | (id1,s1,h1) :: (id2,s2,h2) :: hitTupleList when id1 = id2 -> 
        unionHitFunction ray hitTupleList (h1 :: h2 :: hitList)
    | (id1,s1,h1) :: (id2,s2,h2) :: hitTupleList when id1 <> id2 ->
        let exitTuple = findExitHit hitTupleList
        unionHitFunction ray (fst exitTuple) (h1 :: (snd exitTuple) :: hitList)
    | (id,s,h) :: hitTupleList ->
        unionHitFunction ray hitTupleList (h :: hitList)
    | [] -> hitList

/// <summary>
/// Shoot a ray, and check if it hits the specificed shape.
/// Returns a hitpoint for each point on the shape that
/// was hit, as a list.
/// </summary>
/// <param name="ray">The ray.</param>
/// <param name="shape">The shape.</param>
/// <returns>The list of hitpoints with the ray, on the shape.</returns>
let rec hitFunction ray shape =
    let rayVector = Ray.getVector ray
    let rayOrigin = Ray.getOrigin ray
    match shape with
    | Plane(p0, normal, texture) ->
        let rdn = rayVector * normal // ray and normal dotproduct
        // If the ray and normals' dotproducts are too close, we do not
        // want to render the hit. We render both sides of the plane.
        if rdn < EPSILON && rdn > -EPSILON then List.empty else

        // hit distance traveled
        let t = ((Point.distance rayOrigin p0) * normal) / rdn
        // The hit is behind the camera
        if t < 0. then List.empty else

        // get hit point and its coordinate on the infinite plane
        // TODO: we do not take the y-axis into account (maybe fix later)
        let hitpoint = Point.move rayOrigin (Vector.multScalar rayVector t)
        let (hpx, _, hpz) = Point.getCoord hitpoint

        let u = abs(hpx % 1.0)
        let v = abs(hpz % 1.0)

        // gets material for the hit point
        let material = Texture.getMaterial u v texture
        [Hit(t, normal, material)]
    | Sphere(center, radius, texture) ->
        let (dx, dy, dz) = Vector.getCoord rayVector
        let (ox, oy, oz) = Point.getCoord rayOrigin
        let (x, y, z) = Point.getCoord center
        let a = dx**2. + dy**2. + dz**2.
        let b = 2. * ((ox - x) * dx + (oy - y) * dy + (oz - z) * dz)
        let c = (ox - x)**2. + (oy - y)**2. + (oz - z)**2. - radius**2.
        let distances = distance a b c
        match distances with
        | []         -> List.empty
        | [hp]       -> [sphereDeterminer hp center rayVector rayOrigin texture]
        | [hp1; hp2] -> [sphereDeterminer hp1 center rayVector rayOrigin texture;
                         sphereDeterminer hp2 center rayVector rayOrigin texture]
        | _          -> failwith "Error: Hitting a sphere more than two times!"
    | Triangle(a, b, c, t) ->
        let material =  Texture.getMaterial 0. 0. t

        // Möller-Trumbore intersection algorithm
        let e1 = Point.distance a b
        let e2 = Point.distance a c
        let P = Vector.crossProduct rayVector e2
        let det = Vector.dotProduct e1 P

        if det > -EPSILON && det < EPSILON then List.empty else
        let invDet = 1. / det

        let T = Point.distance a rayOrigin

        let u = (Vector.dotProduct T P) * invDet
        if u < 0. || u > 1. then List.empty else

        let Q = Vector.crossProduct T e1

        let v = (Vector.dotProduct rayVector Q) * invDet
        if v < 0. || (u + v) > 1. then List.empty else

        let uvcp = Vector.crossProduct e1 e2
        let n = Vector.multScalar uvcp (1. / (Vector.magnitude (uvcp)))

        let t = (Vector.dotProduct e2 Q) * invDet
        if t > EPSILON
        then [Hit(t, n, material)]
        else List.empty
    | Composite(shape1, shape2, composition) ->
        let hitTupleList = sortToTuples shape1 shape2 (hitFunction ray shape1) (hitFunction ray shape2)
        match composition with
        | Union ->
            unionHitFunction ray hitTupleList []
        | Subtraction ->
            failwith "Subtraction is not implemented yet"
        | Intersection ->
            failwith "Intersection is not implemented yet"
    | _ ->
        failwith "No hit function for this shape"
