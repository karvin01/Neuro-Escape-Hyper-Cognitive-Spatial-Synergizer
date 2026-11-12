Element Pillar (URP) for Occult Stone Pillars / Stone_Pillar_Sqr_b
=================================================================

1. Copy this ElementPillar folder anywhere under Assets/ (keep its subfolders).
2. In Unity: Tools > Element Pillar > Build Prefabs
3. Two prefab variants appear next to this README:
     Stone_Pillar_Sqr_b_Elements        carved stone medallions
     Stone_Pillar_Sqr_b_Elements_Color  same, with coloured inlays

Sides (Unity, pillar local space). Opposites face each other:
     Water  -Z     Fire  +Z     Wind  +X     Rock  -X

Medallions: 200 mm diameter, 18 mm proud of the shaft, centred at 2.08 m.
They are added to LOD0-LOD2 of the pillar's LODGroup; the original LOD distances are kept.

What the script creates:
  Materials/Stone_Pillar_Sqr_b_URP.mat      URP Lit using the original pillar textures
  Materials/Medallion_<Element>.mat          carved (4)
  Materials/Medallion_<Element>_ColorInlay.mat (4)
  Meshes/Medallion.asset

The original prefab, FBX, material and textures are not changed.
To move or resize the medallions, edit the constants at the top of
Editor/ElementPillarBuilder.cs and run the menu item again. It overwrites its own output.
