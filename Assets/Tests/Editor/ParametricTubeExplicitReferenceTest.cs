using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    // ParametricBloomFogLightController must drive only its explicitly serialized
    // SpriteLight/BoxLight references. Native TubeBloomPrePassLight has no implicit
    // child adoption, so unreferenced parametric children stay untouched.
    public class ParametricTubeExplicitReferenceTest
    {
        private static GameObject BuildTube(
            out ParametricBloomFogLightController root,
            out ParametricSpriteLight sprite,
            out ParametricBoxLight box)
        {
            var rootGo = new GameObject("TubeRoot");
            root = rootGo.AddComponent<ParametricBloomFogLightController>();

            var spriteGo = new GameObject("SpriteChild");
            spriteGo.transform.SetParent(rootGo.transform, false);
            sprite = spriteGo.AddComponent<ParametricSpriteLight>();
            sprite.Renderer = spriteGo.AddComponent<MeshRenderer>();

            var boxGo = new GameObject("BoxChild");
            boxGo.transform.SetParent(rootGo.transform, false);
            box = boxGo.AddComponent<ParametricBoxLight>();
            box.Renderer = boxGo.AddComponent<MeshRenderer>();
            return rootGo;
        }

        private static void SeedChildBlue(ParametricSpriteLight sprite, ParametricBoxLight box)
        {
            sprite.InitIfNeeded();
            box.InitIfNeeded();
            sprite.SetColor(Color.blue);
            box.SetColor(Color.blue);
            sprite.Renderer.enabled = false;
            box.Renderer.enabled = false;
        }

        private static Color ReadColor(Renderer renderer)
        {
            var mpb = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(mpb);
            return mpb.GetColor("_Color");
        }

        private static void AssertUntouched(ParametricSpriteLight sprite, ParametricBoxLight box)
        {
            var spriteColor = ReadColor(sprite.Renderer);
            Assert.That(spriteColor.b, Is.GreaterThan(0.9f), "Unreferenced sprite child must keep its color.");
            Assert.That(spriteColor.r, Is.LessThan(0.1f));
            var boxColor = ReadColor(box.Renderer);
            Assert.That(boxColor.b, Is.GreaterThan(0.9f), "Unreferenced box child must keep its color.");
            Assert.That(boxColor.r, Is.LessThan(0.1f));
            Assert.That(sprite.Renderer.enabled, Is.False, "Unreferenced sprite renderer must stay disabled.");
            Assert.That(box.Renderer.enabled, Is.False, "Unreferenced box renderer must stay disabled.");
            Assert.That(sprite.UseCollision, Is.False, "Unreferenced sprite must not take collision state.");
            Assert.That(box.UseCollision, Is.False, "Unreferenced box must not take collision state.");
        }

        private static void AssertDriven(ParametricSpriteLight sprite, ParametricBoxLight box)
        {
            var spriteColor = ReadColor(sprite.Renderer);
            Assert.That(spriteColor.r, Is.GreaterThan(0.9f), "Referenced sprite child must receive the light color.");
            Assert.That(spriteColor.g, Is.LessThan(0.1f));
            Assert.That(spriteColor.b, Is.LessThan(0.1f));
            var boxColor = ReadColor(box.Renderer);
            Assert.That(boxColor.r, Is.GreaterThan(0.9f), "Referenced box child must receive the light color.");
            Assert.That(boxColor.g, Is.LessThan(0.1f));
            Assert.That(boxColor.b, Is.LessThan(0.1f));
            Assert.That(sprite.Renderer.enabled, Is.True, "Referenced sprite renderer must follow EnableRenderers.");
            Assert.That(box.Renderer.enabled, Is.True, "Referenced box renderer must follow EnableRenderers.");
            Assert.That(sprite.UseCollision, Is.True, "Referenced sprite must take collision state.");
            Assert.That(box.UseCollision, Is.True, "Referenced box must take collision state.");
        }

        [Test]
        public void UnreferencedChildrenAreNotDriven()
        {
            var rootGo = BuildTube(out var root, out var sprite, out var box);
            try
            {
                SeedChildBlue(sprite, box);

                root.Start();
                root.UseCollision = true;
                root.SetColor(Color.red);

                AssertUntouched(sprite, box);
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }

        [Test]
        public void ExplicitlyReferencedChildrenAreDriven()
        {
            var rootGo = BuildTube(out var root, out var sprite, out var box);
            try
            {
                SeedChildBlue(sprite, box);
                root.SpriteLight = sprite;
                root.BoxLight = box;

                root.Start();
                root.UseCollision = true;
                root.SetColor(Color.red);

                AssertDriven(sprite, box);
            }
            finally
            {
                Object.DestroyImmediate(rootGo);
            }
        }
    }
}
