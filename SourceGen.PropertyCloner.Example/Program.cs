using PropertyCloner;
using MyNamespace;



MyClassA myClass = new() { Color = Color.Red, Size = Size.Large };
var clone = (MyClassA)myClass.Clone();
clone.Color = Color.Blue;

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

    public abstract class BaseClass
    {
        [Clonable] public Shape Shape { get; set; }

        public abstract BaseClass Clone();
    }

    [PropertyCloner]
    public class MyClassA : BaseClass
    {
        [Clonable] public Color Color { get; set; }
        public Size Size { get; set; }

        public override BaseClass Clone() => this.CloneProperties();
    }

    [PropertyCloner]
    public class MyClassB : BaseClass
    {
        public Color Color { get; set; }
        [Clonable] public Size Size { get; set; }
        [Clonable] public int Number { get; set; }

        public override BaseClass Clone()
        {
            return this.CloneProperties();
        }


    }
}

