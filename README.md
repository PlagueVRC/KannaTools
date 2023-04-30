# KannaTools
KannaTools is a bunch of scripts for Unity for use with VRChat avatars. The main use case is for updating an avatar to a new FBX build easier. This solves FBX updates breaking your avatar causing hours of work to remake it on the new FBX.

| Script Name | Use Case |
| :------------------- | :----------: |
| FBXUpdater           | For copying all materials and main VRChat components like the avatar descriptor to a new FBX. This is added as a component to the old avatar root object. |
| SelectUselessMaterials              | Used when right clicking your materials folder. This will select all materials not used in your scene in any Renderer. You can then click the cog at the top right of the inspector and click select materials for moving them elsewhere, for example. |
| BlindCopyMaterials | This is best used after using FBXUpdater. If FBXUpdater fails to copy materials for a mesh, you can use this to copy and paste the materials to the new mesh blindly in the hope it works. Note this will only work in cases such as a added material at the end of the materials for said mesh. If the general order of materials changed, this will not work. |
| BlindCopyTransforms | This is best used after FBXUpdater. This is used for moving all un-found transforms in the new FBX over to it blindly. Note to be sure you didn't rename any bones or such manually or with cats blender plugin, or this will copy them over stupidly. Only use this if no names of any meshes or bones have changed. In that case, it is the most useful thing ever to get back up and going. |
