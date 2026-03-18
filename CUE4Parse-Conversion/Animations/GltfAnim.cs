using System;
using System.Collections.Generic;
using System.Numerics;
using CUE4Parse_Conversion.Animations.PSA;
using CUE4Parse_Conversion.Meshes;
using CUE4Parse_Conversion.Meshes.glTF;
using CUE4Parse.UE4.Objects.Core.Math;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;

namespace CUE4Parse_Conversion.Animations;

public class GltfAnim
{
    public readonly ModelRoot Model;

    public GltfAnim(CAnimSet animSet, int sequenceIndex)
    {
        var sequence = animSet.Sequences[sequenceIndex];
        var numBones = animSet.Skeleton.BoneCount;
        var fps = sequence.FramesPerSecond;

        // Build skeleton bone list from USkeleton (same conversion as mesh export)
        animSet.Skeleton.TryConvert(out var bones, out _);

        // Create scene and build bone hierarchy
        var sceneBuilder = new SceneBuilder();
        var armatureNode = new NodeBuilder(sequence.Name + ".ao");
        var boneNodes = Gltf.CreateGltfSkeleton(bones, armatureNode);

        // Map bone names to NodeBuilder for animation targeting
        var boneNodeMap = new Dictionary<string, NodeBuilder>();
        foreach (var node in boneNodes)
            boneNodeMap[node.Name] = node;

        var trackName = sequence.Name;

        for (int boneIndex = 0; boneIndex < numBones; boneIndex++)
        {
            var boneName = animSet.Skeleton.ReferenceSkeleton.FinalRefBoneInfo[boneIndex].Name.Text;
            if (!boneNodeMap.TryGetValue(boneName, out var node))
                continue;

            if (sequence.OriginalSequence.FindTrackForBoneIndex(boneIndex) < 0)
                continue;

            var track = sequence.Tracks[boneIndex];
            if (!track.HasKeys())
                continue;

            // Reference pose as default (same values CreateGltfSkeleton used)
            var refPos = bones[boneIndex].Position;
            var refRot = bones[boneIndex].Orientation;

            var posKeys = new Dictionary<float, Vector3>();
            var rotKeys = new Dictionary<float, Quaternion>();

            for (int frame = 0; frame < sequence.NumFrames; frame++)
            {
                var time = frame / fps;
                var pos = refPos;
                var rot = refRot;
                var scale = FVector.OneVector;

                track.GetBoneTransform(frame, sequence.NumFrames, ref rot, ref pos, ref scale);

                posKeys[time] = Gltf.SwapYZ(pos * 0.01f);
                rotKeys[time] = Gltf.SwapYZ(rot);
            }

            node.WithLocalTranslation(trackName, posKeys);
            node.WithLocalRotation(trackName, rotKeys);

            // Only add scale channel if animation has non-trivial scale data
            if (track.KeyScale.Length > 1 ||
                (track.KeyScale.Length == 1 && !IsUnitScale(track.KeyScale[0])))
            {
                var scaleKeys = new Dictionary<float, Vector3>();
                for (int frame = 0; frame < sequence.NumFrames; frame++)
                {
                    var time = frame / fps;
                    var pos = refPos;
                    var rot = refRot;
                    var scale = FVector.OneVector;
                    track.GetBoneTransform(frame, sequence.NumFrames, ref rot, ref pos, ref scale);
                    scaleKeys[time] = scale;
                }
                node.WithLocalScale(trackName, scaleKeys);
            }
        }

        sceneBuilder.AddNode(armatureNode);
        Model = sceneBuilder.ToGltf2();
    }

    private static bool IsUnitScale(FVector scale)
    {
        return MathF.Abs(scale.X - 1f) < 0.001f &&
               MathF.Abs(scale.Y - 1f) < 0.001f &&
               MathF.Abs(scale.Z - 1f) < 0.001f;
    }
}
