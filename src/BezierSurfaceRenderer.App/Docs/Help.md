# Bézier Surface Renderer

The program displays bicubic Bézier surfaces loaded from a text file as triangle meshes, filled in software with per-pixel lighting, a depth buffer (Z-buffer), a texture and a normal map. Press **F1** or **Esc** to close this window.

## 1. Quick start

- The surfaces are loaded at startup from `Assets/surfaces.txt` (next to the executable). To load other surfaces, replace that file.
- **Dragging with the left mouse button** in the drawing area rotates the scene.
- The panel on the right changes the triangulation accuracy, what is drawn, the material, the object color, the normal map and the light.

## 2. Mouse controls

| Action | Effect |
|---|---|
| Left-button drag | Free rotation (virtual sphere, trackball) — the point under the cursor follows the cursor |
| Drag near the edge / outside the sphere | Rotation about the viewing axis (z axis) |
| Dragging a light path point | Moves the control point (only in "Edit light path" mode) |

Rotation angles are not limited in any way — the scene can be rotated any number of times in any direction.

## 3. Keyboard shortcuts

| Key | Action |
|---|---|
| F1 | Opens the documentation |
| Space | Starts / stops the light animation |
| E | Toggles light path editing mode (mouse rotation is disabled while it is on) |
| R | Restores the initial scene orientation |
| 1 | Shows / hides the Bézier polygons (control nets) |
| 2 | Shows / hides the triangle mesh |
| 3 | Shows / hides the filled triangles |
| Arrows, PgUp/PgDn, Home/End, mouse wheel | Change the value of the focused slider |

## 4. Control panel

### Geometry
- **Triangulation** — number of mesh cells (u, v) per side, from 1 to 64. Each cell is split into two triangles, so one surface has 2·n² triangles.
- **Bézier control net** — the Bézier polygon (4×4 grid of control points) of each surface (orange).
- **Triangle mesh** — triangle edges (cyan).
- **Filled triangles** — filled, lit triangles with a depth buffer.

### Material
- **kd**, **ks** (0–1) and **m** (1–100) — lighting model coefficients, the same for all triangles.

### Surface (color, normal map) — object color IO and normal map
Both are chosen **independently for each surface**: first pick a surface from the "Surface to edit" list; the remaining controls in this section apply only to it (e.g. one surface red, the other textured; one with the brick map, the other with the sphere map or no map).
- **Object color IO** — a **solid color** (menu **Colors → Object color** or the "Color…" button; the swatch next to the button shows the current color) or a **texture**. The texture is scaled to the parameter domain: point P(u, v) takes the color of texel (u·width, v·height). A default texture is loaded at startup (the second surface starts textured, the first with a solid color); another one can be loaded with "Load texture…".
- **Normal map** — modifies the normal vector using the selected surface's normal map (defaults: first surface — bricks, second — sphere; both disabled). Another map can be loaded with "Load normal map…".

### Light
- **Light z** — z coordinate of the plane in which the light moves.
- **Light color…** (also menu **Colors → Light color**) — light color IL, white by default (the swatch next to the button shows the current color). Applies to single-light mode.
- **Animate light** — the light moves along a closed curve (Bézier or B-spline); unchecking it (or Space) stops the animation.
- **Edit light path** — shows the curve's control polygon (points V0 … V7; the larger square is V0 = V8). Every point can be dragged with the mouse. The circle on the curve is the current light position (yellow for the white light; red, green and blue in three-light mode).
- **B-spline path (Bézier segments)** — switches the curve type. Off: a single Bézier curve of degree 8. On: the same points V0 … V7 are the vertices of a closed B-spline control polygon, and the cubic B-spline is built geometrically from 8 Bézier segments; each segment is drawn in a different color, and the light passes from one segment to the next. In "Edit light path" mode the segment control points are also shown (dots in the segment colors).
- **Three lights (R, G, B)** — switches the number of lights. Off: one light. On: three point lights with fixed colors red (255, 0, 0), green (0, 255, 0) and blue (0, 0, 255) travel along the same curve (Bézier or B-spline), offset from each other by 1/3 of a lap (evenly in the parameter t, so only approximately evenly along the curve). Each light's position is marked by a circle in its color.
- **Spotlight focus p (0 = point)** — (0–200, default 50) applies to the three R, G, B lights. For p > 0 they are **spotlights** aimed at the point (0, 0, 0) (the scene centre); the larger p, the narrower the beam. A short line next to the circle shows the beam direction. For p = 0 they are ordinary point lights.

## 5. Input file format

Each surface is 16 consecutive lines `x y z` (real numbers, dot as the decimal separator) in the order V00, V01, V02, V03, V10, …, V33: line k holds point V(i, j), where i = k / 4 (corresponds to parameter u) and j = k mod 4 (corresponds to parameter v). Surfaces follow one another in groups of 16 lines; at least two are required. Blank lines are skipped. The bundled file contains a "dome" and a wavy "saddle"; both surfaces are functions over the xy plane, and their control points fill the cube [−1, 1]³ centred at (0, 0, 0).

## 6. Mapping mouse motion to object rotation (technical description)

### 6.1 Incremental orientation composition
The scene stores a single 3×3 orientation matrix R (column-vector convention, P' = R·P), initially Rx(−0.9)·Rz(0.6) (a slight tilt so the surfaces look three-dimensional right away; the R key restores this view). Every mouse-move event during a drag produces a small rotation R_inc, computed only from the **previous** and the **current** cursor position, and composes it with the current orientation:

```
R_new = R_inc · R_current
R_new = orthonormalize(R_new)        (Gram–Schmidt)
```

So the orientation is never derived from the position where the drag started, and the angles are not limited. Because R_inc is multiplied on the left, it acts in screen (viewer) space: the same mouse motion always rotates the object the same way relative to the screen, no matter how it is already rotated. Thousands of floating-point multiplications gradually break the matrix's orthogonality, so it is re-orthonormalized after every step (the first column is normalized, the second is orthogonalized against the first and normalized, the third is their cross product).

Mesh vertices are always rotated from the original, unrotated data with the composed matrix R: P' = R·P, Pu' = R·Pu, Pv' = R·Pv, N' = normalize(R·N). Points are rotated about the scene centre (0, 0, 0), tangent and normal vectors as direction vectors.

### 6.2 Virtual sphere (left-button drag)
We treat the drawing area as if a glass sphere of radius r = ½·min(width, height) sat at its centre, and the user rolls its surface with the cursor. The cursor position relative to the centre of the area (y axis up) p = (x, y) is projected onto the sphere:

```
q = p / r
if |q|² ≤ 1:   s = (qx, qy, sqrt(1 − |q|²))     point on the front hemisphere
otherwise:     s = (qx, qy, 0) / |q|           point on the rim (silhouette) of the sphere
```

For two consecutive cursor positions p0 → p1, projected to sphere points s0, s1:

```
axis  = normalize(s0 × s1)
angle = acos(clamp(s0 · s1, −1, 1))
R_inc = rotation about axis by angle               (Rodrigues' formula)
```

(If |s0 × s1| = 0 the mouse did not move and R_inc = I.) This is the shortest rotation taking s0 to s1, so the sphere point under the cursor follows the cursor exactly — which makes the control intuitive:

- horizontal motion through the centre: s0 × s1 is parallel to the y axis → the object turns left/right about the vertical axis;
- vertical motion: rotation about the horizontal x axis → the object tilts towards or away from the viewer;
- diagonal motion: rotation about the axis perpendicular to the direction of motion;
- motion along the outer part of the area: both points lie on the sphere's rim (z = 0), so s0 × s1 is parallel to the z axis → the object rotates in the screen plane, like a steering wheel.

Rodrigues' formula for a unit axis a = (x, y, z), c = cos θ, s = sin θ, t = 1 − c:

```
      | t·x·x + c     t·x·y − s·z   t·x·z + s·y |
R  =  | t·x·y + s·z   t·y·y + c     t·y·z − s·x |
      | t·x·z − s·y   t·y·z + s·x   t·z·z + c   |
```

For a equal to the x, y or z axis, the formula reduces exactly to the standard Rx, Ry, Rz rotation matrices.

### 6.3 Worked example
Drawing area 800×800 (r = 400). The cursor moves from the centre p0 = (0, 0) to p1 = (40, 0):
s0 = (0, 0, 1), s1 = (0.1, 0, 0.995); s0 × s1 = (0, 0.1, 0), so the axis is +y and the angle is acos(0.995) ≈ 5.7°. R_inc = Ry(5.7°): the front of the object moves right together with the cursor.

## 7. Rendering pipeline (technical description)

- **Surface**: P(u,v) = Σ Σ V(i,j)·B(i,3)(u)·B(j,3)(v); Pu and Pv are the exact partial derivatives (degree-2 Bernstein form); N = normalize(Pu × Pv).
- **Triangulation**: a regular (n+1)×(n+1) grid in the (u, v) domain, two triangles per cell; each vertex stores P, Pu, Pv, N before and after rotation, plus the parameters (u, v).
- **Projection**: orthographic projection onto the xy plane; the origin is at the centre of the drawing area, the y axis points up. The scale is chosen so that the sphere containing all control points fits in the drawing area, so the surfaces fit at any rotation.
- **Filling**: each triangle is filled independently with a scan-line algorithm with bucket-sorted edges (edge table ET with buckets indexed by the first scan line; active edge table AET sorted by x on every line). Pixel centres are sampled with the top-left rule, so adjacent triangles neither overlap nor leave gaps.
- **Pixel color**: for every pixel the barycentric coordinates in the projected triangle are computed (exact for orthographic projection) and used to interpolate z, N, Pu, Pv, u and v. N is re-normalized.
- **Depth buffer (Z-buffer)**: the viewer looks from the +z direction (V = [0, 0, 1]); a pixel is written only if its z is greater (closer to the viewer) than the value stored in the buffer.
- **Normal map** (optional): N_tex = 2·RGB/255 − 1 (normalized), M = [Pu, Pv, N] (columns; Pu and Pv normalized), N = normalize(M · N_tex).
- **Lighting**, separately for the R, G, B components, values in 0–1:

```
I = kd·IL·IO·cos(N, L) + ks·IL·IO·cos^m(V, R)
L = normalize(light − P),  V = [0, 0, 1],  R = 2⟨N, L⟩N − L
```

  for a spotlight IL is replaced by IL·cos^p(α), where α is the angle between the spotlight axis D = normalize((0, 0, 0) − spotlight position) and the direction from the spotlight to point P, i.e. −L (Warn model): cos α = ⟨−L, D⟩, and for cos α ≤ 0 the point is not lit. On the beam axis the light has full intensity, and the larger p, the faster it falls off away from the axis. With several lights the term above is computed for each of them separately (with its own position and color IL) and the results are summed: I = I₁ + I₂ + I₃. **Both sides of the surface** are lit: the normal is flipped towards the viewer (if N_z < 0 it is replaced by −N), so the underside is lit just like the top. If the light is on the other side of the surface than the viewer, the visible side is not lit (no diffuse term and no highlight). Only the final result is multiplied by 255 and clamped to 255.
- **Light**: a point light (or three R, G, B lights) moving along a closed Bézier curve of degree 8 (control points V0 … V8, V8 = V0, i.e. 9 points, 8 of them distinct) or along a B-spline built on the same 8 points (see below) in the plane z = const, in viewer space (the curve does not rotate with the scene). The curve point for parameter t is computed with de Casteljau's algorithm; t advances at 0.1 lap per second.
- **B-spline (geometric construction)**: every side V(i)V(i+1) of the closed polygon is divided into three equal parts by points A(i) (⅓) and B(i) (⅔); at each vertex V(i) we join the neighbouring division points B(i−1) and A(i) and halve that segment — its midpoint is S(i). Segment i is a cubic Bézier curve with control points S(i), A(i), B(i), S(i+1), so 8 vertices give 8 segments (indices modulo 8). The parameter t ∈ [0, 1) selects segment k = ⌊8t⌋ and the local parameter 8t − k; the segment point is computed with de Casteljau's algorithm.
- **Performance**: horizontal bands of the image are rendered in parallel; each band owns its rows of the color buffer and the depth buffer exclusively.
