# Bézier Surface Renderer

Renders bicubic Bézier surfaces read from a text file as triangle meshes, filled by a software rasteriser with per-pixel lighting, a Z-buffer, textures and normal maps. Press **F1** or **Esc** to close this window.

## 1. Quick start

- The surfaces are loaded at start-up from `Assets/surfaces.txt` (next to the program). Another file can be given as the first command-line argument: `BezierSurfaceRenderer.exe my_surfaces.txt`.
- **Drag with the left mouse button** in the drawing area to rotate the scene.
- Use the panel on the right to change triangulation, the visible elements, the material, the object colour, the normal map and the light.

## 2. Mouse controls

| Action | Effect |
|---|---|
| Left drag | Free rotation (virtual trackball) — the point under the cursor follows the cursor |
| Left drag near/outside the edge | Rotation about the viewing axis (roll) |
| Shift + left drag, or right drag | Pure roll about the viewing axis |
| Left drag on a light-path point | Moves that control point (only in "Edit light path" mode) |
| Mouse wheel on a slider | Changes the slider by one step |

There is no limit on rotation angles; the scene can be turned any number of times in any direction.

## 3. Keyboard shortcuts

| Key | Action |
|---|---|
| F1 | Open this documentation |
| Space | Start / stop the light animation |
| E | Toggle "Edit light path" mode (mouse rotation is disabled while editing) |
| R | Reset the orientation |
| 1 | Show / hide the Bézier control net (polygon) |
| 2 | Show / hide the triangle mesh |
| 3 | Show / hide the filled triangles |
| T | Switch object colour: solid colour / texture |
| N | Normal map on / off |
| [ and ] | Decrease / increase triangulation density |
| Ctrl+T | Load a texture |
| Ctrl+N | Load a normal map |
| Ctrl+L | Choose the light colour IL |
| Ctrl+K | Choose the solid object colour IO |
| Arrows, PgUp/PgDn, Home/End | Adjust the focused slider |

## 4. Control panel

### Geometry
- **Triangulation** — number of cells per side of the (u, v) grid, 1–64. Each cell is split into two triangles, so one surface has 2·n² triangles.
- **Bézier control net** — the 4×4 control polygon of every surface (amber).
- **Triangle mesh** — triangle edges (cyan).
- **Filled triangles** — the shaded, Z-buffered surface.

### Material
- **kd**, **ks** (0–1) and **m** (1–100) — coefficients of the lighting model, equal for all triangles.
- **Object color IO** — either a **solid colour** (swatch or Colors menu) or a **texture**. The texture is stretched over the parameter domain: texel (u·width, v·height) colours the point P(u, v). A default texture is loaded at start-up; another one can be loaded with "Load…".

### Surface detail
- **Normal map** — perturbs the normal using the loaded normal map (default: bricks; a sphere map is also in `Assets/normalmaps`).

### Light
- **Light z** — height of the plane in which the light moves.
- **Light color IL** — default white.
- **Animate light** — the light travels around a closed Bézier curve; uncheck (or press Space) to stop it.
- **Edit light path** — shows the curve's control points: squares lie on the curve, circles are tangent handles. Dragging a square moves it together with its handles; dragging a handle mirrors the opposite handle, so the curve stays smooth (C1). The bright dot is the current light position.

The HUD in the top-left corner shows the frame time, triangle count, light position and animation state. The gizmo in the bottom-left corner shows the current directions of the x (red), y (green) and z (blue) axes.

## 5. Input file format

Each surface is 16 consecutive lines `x y z` (real numbers, `.` as the decimal separator), in the order V00, V01, V02, V03, V10, … V33: line k holds V(i, j) with i = k / 4 (paired with parameter u) and j = k mod 4 (paired with v). Surfaces follow each other in groups of 16 lines; at least two are required. Blank lines are ignored. The shipped file contains a dome and a wavy saddle; both are functions over the xy plane and their control points span the cube [−1, 1]³ centred at (0, 0, 0).

## 6. How mouse movement becomes a rotation (technical description)

### 6.1 Orientation is accumulated incrementally
The scene keeps one 3×3 orientation matrix R (column-vector convention, P' = R·P), starting from the identity. Every mouse-move event during a drag produces a small rotation R_inc computed only from the **previous** and the **current** cursor positions, and it is composed with the current orientation:

```
R_new = R_inc · R_current
R_new = orthonormalize(R_new)        (Gram–Schmidt)
```

The orientation is therefore never recomputed from the position where the drag started, and no angle is ever clamped. Because R_inc is applied on the left, it acts in screen (view) space: dragging always turns the object the same way on screen, regardless of how it is already rotated. Thousands of float multiplications slowly destroy orthogonality, so the product is re-orthonormalised after every step (the first column is normalised, the second is made orthogonal to it and normalised, the third is their cross product).

Mesh vertices are always rotated from their original, unrotated data with the accumulated R: P' = R·P, Pu' = R·Pu, Pv' = R·Pv, N' = normalize(R·N). Points rotate about the scene centre (0, 0, 0); tangents and normals are direction vectors.

### 6.2 Virtual trackball (left drag)
The drawing area is treated as if a glass ball of radius r = ½·min(width, height) sat in its centre, and the user drags its surface. A cursor position (relative to the centre, y up) p = (x, y) is mapped onto the ball:

```
q = p / r
if |q|² ≤ 1:   s = (qx, qy, sqrt(1 − |q|²))        point on the front hemisphere
else:          s = (qx, qy, 0) / |q|               point on the silhouette circle
```

For two consecutive cursor positions p0 → p1 with sphere points s0, s1:

```
axis  = normalize(s0 × s1)
angle = acos(clamp(s0 · s1, −1, 1))
R_inc = rotation about axis by angle            (Rodrigues' formula)
```

(If |s0 × s1| is zero the mouse did not move and R_inc = I.) This is the shortest rotation taking s0 to s1, so the point of the ball under the cursor exactly follows the cursor — which is what makes the control intuitive:

- horizontal drag through the centre: s0 × s1 is parallel to the y axis → the object turns left/right about the vertical axis;
- vertical drag: rotation about the horizontal x axis → the object tips towards / away from the viewer;
- diagonal drag: rotation about the axis perpendicular to the drag direction;
- drag along the outer part of the area: both points lie on the silhouette (z = 0), so s0 × s1 is parallel to z → the object spins in the screen plane, like turning a steering wheel.

Rodrigues' formula for a unit axis a = (x, y, z), c = cos θ, s = sin θ, t = 1 − c:

```
      | t·x·x + c     t·x·y − s·z   t·x·z + s·y |
R  =  | t·x·y + s·z   t·y·y + c     t·y·z − s·x |
      | t·x·z − s·y   t·y·z + s·x   t·z·z + c   |
```

For a = x, y or z it reduces exactly to the matrices Rx, Ry, Rz of the assignment.

### 6.3 Roll (Shift + left drag or right drag)
The angle swept by the cursor around the centre of the drawing area between two consecutive positions is applied as a rotation about the viewing axis:

```
angle = atan2(p0 × p1, p0 · p1)       (2D cross and dot product)
R_inc = Rz(angle)
```

Counter-clockwise mouse motion rotates the scene counter-clockwise.

### 6.4 Worked example
Drawing area 800×800 (r = 400). The cursor moves from the centre p0 = (0, 0) to p1 = (40, 0):
s0 = (0, 0, 1), s1 = (0.1, 0, 0.995); s0 × s1 = (0, 0.1, 0), so the axis is +y and the angle is acos(0.995) ≈ 5.7°. R_inc = Ry(5.7°): the front of the object moves to the right, together with the cursor.

## 7. Rendering pipeline (technical summary)

- **Surface**: P(u,v) = Σ Σ V(i,j)·B(i,3)(u)·B(j,3)(v); Pu and Pv are the exact partial derivatives (degree-2 Bernstein form); N = normalize(Pu × Pv).
- **Triangulation**: a uniform (n+1)×(n+1) grid in (u, v), two triangles per cell; each vertex stores P, Pu, Pv, N before and after rotation and its (u, v).
- **Projection**: orthographic onto the xy plane; the origin is the centre of the drawing area and y points up. The scale makes the sphere around all control points fit the area, so the surfaces fit at every rotation.
- **Filling**: every triangle is filled independently with the scan-line algorithm with a bucket-sorted edge table (ET buckets indexed by the first scan line; the active edge table is sorted by x on every line). Pixel centres are sampled with the top-left rule, so neighbouring triangles neither overlap nor leave gaps.
- **Per-pixel colour**: for each pixel the barycentric coordinates in the projected triangle are computed (exact for an orthographic projection) and used to interpolate z, N, Pu, Pv, u and v. N is renormalised.
- **Z-buffer**: the observer looks from +z (V = [0, 0, 1]); a pixel is written only if its z is larger (nearer) than the stored value.
- **Normal map** (optional): N_tex = 2·RGB/255 − 1 (normalised), M = [Pu, Pv, N] (columns, Pu and Pv normalised), N = normalize(M · N_tex).
- **Lighting**, separately for R, G, B with values in 0–1:

```
I = kd·IL·IO·cos(N, L) + ks·IL·IO·cos^m(V, R)
L = normalize(light − P),  V = [0, 0, 1],  R = 2⟨N, L⟩N − L
```

  negative cosines are replaced by 0; only the final result is multiplied by 255 and clamped to 255.
- **Light**: a point light moving along a closed curve of four cubic Bézier segments in the plane z = const, in view space (it does not rotate with the scene).
- **Performance**: horizontal bands of the image are rendered in parallel; each band owns its rows of the colour and depth buffers.
