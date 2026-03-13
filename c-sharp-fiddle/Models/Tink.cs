namespace c_sharp_fiddle.Models
{
    internal class Tink : ITink, IComparable<Tink>
    {
        internal Tink()
        {
            this.Name = string.Empty;
            this.Description = string.Empty;
            this.Image = string.Empty;
        }

        public override string ToString()
        {
            return $"Name: {this.Name}\nDescription: {this.Description}\nImage URL: {this.Image}";
        }

        public int CompareTo(Tink? other)
        {
            return this.Name.CompareTo(other?.Name);
        }

        public string Name { get; set; }
        public string Description { get; set; }
        public string Image { get; set; }
    }

    internal interface ITink
    {
    }
}

