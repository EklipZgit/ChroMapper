using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Tests.Editor
{
    public class MultiSettingsSerializationTest
    {
        [Test]
        public void ConstructorDoesNotRequireTheUnityMainThread()
        {
            MultiSettings settings = null;
            Assert.DoesNotThrow(() =>
            {
                settings = Task.Run(() => new MultiSettings()).GetAwaiter().GetResult();
            }, "Settings constructed during scene serialization must not call Unity's random API.");

            var color = settings.GridColor;
            Assert.That(Mathf.Max(color.r, color.g, color.b), Is.EqualTo(1f).Within(0.00001f));
            Assert.That(Mathf.Min(color.r, color.g, color.b), Is.EqualTo(0f).Within(0.00001f));
            Assert.That(color.a, Is.EqualTo(1f));
        }
    }
}
