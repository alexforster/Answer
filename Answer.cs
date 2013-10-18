namespace Answer
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Text;

	public sealed class Answer: IEnumerable<Answer>
	{
		public String Name;

		public String Value;

		public Int32 Indent;

		public Answer Parent; // obvious cycle is obvious

		public readonly List<Answer> Children = new List<Answer>();

		public Int32 Depth
		{
			get
			{
				var depth = 0;
				var current = this;
				while( current.Parent != null )
				{
					depth++;
					current = current.Parent;
				}
				return depth;
			}
		}

		public String Serialize()
		{
			var sb = new StringBuilder( this.Name + " " + this.Value );

			sb.Insert( 0, "    ", this.Depth );

			foreach( var child in this.Children )
			{
				sb.AppendLine();
				sb.Append( child.Serialize() );
			}

			return sb.ToString();
		}

		public IEnumerator<Answer> GetEnumerator()
		{
			return this.Children.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public override String ToString()
		{
			return String.IsNullOrEmpty( this.Value ) ? this.Name : this.Value;
		}
	}
}
