using PropertyCloner;
using MyNamespace;



MyClass myClass = new() { Color = Color.Red, Size = Size.Large };
var clone = myClass.Clone();
clone.Color = Color.Green;

namespace MyNamespace
{
    public enum Color
    {
        Red,
        Green,
        Blue,
    }

    public enum Size
    {
        Small,
        Medium,
        Large,
    }

    public enum Shape
    {
        Circle,
        Square,
        Triangle,
    }

    public class BaseClass
    {
        [Clonable] public Shape Shape { get; set; }
    }

    [PropertyCloner]
    public class MyClass : BaseClass
    {
        [Clonable] public Color Color { get; set; }
        public Size Size { get; set; }
    }
}

