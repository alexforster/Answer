namespace Answer
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;
	using System.Text.RegularExpressions;

	public sealed class AnswerParser
	{
		private static readonly Regex TokenizerRegex =
			new Regex( @"^(?<indent>[\t]*|[\ ]*)(?<name>[A-z][A-z0-9_-]*)(?:\s+(?<value>.+)|\s+)??$", RegexOptions.Compiled );

		private static readonly Regex LineSplitterRegex = new Regex( @"\r\n", RegexOptions.Compiled );

		private readonly Stack<Answer> ChildAnswers = new Stack<Answer>();

		private TabsOrSpaces IndentationStyle;

		private enum TabsOrSpaces
		{
			None,
			Tabs,
			Spaces
		}

		public readonly List<Answer> Answers = new List<Answer>();

		public void ParseLine( String rawLine )
		{
			// skip comments

			if( String.IsNullOrEmpty( rawLine ) ||
				rawLine.TrimStart( ' ', '\t' ).StartsWith( "!" ) ||
				rawLine.TrimStart( ' ', '\t' ).StartsWith( "#" ) ||
				rawLine.TrimStart( ' ', '\t' ).StartsWith( "//" ) )
			{
				return;
			}

			// process includes (must start at the beginning of a line; can not be nested)

			if( ( rawLine.StartsWith( "@include " ) ||
				  rawLine.StartsWith( "@include\t" ) ) &&
				  rawLine.Length > 9 )
			{
				var includeFile = new FileInfo( rawLine.Substring( 9 ).Trim() );

				if( includeFile.Exists )
				{
					using( var includeFileReader = includeFile.OpenText() )
					{
						this.Parse( includeFileReader.ReadToEnd() );
						return; // then skip this line
					}
				}
			}

			// clean up the line, and then tokenize it into a key/value pair, 

			rawLine = rawLine.TrimEnd( ' ', '	' );

			var tokenMatch = TokenizerRegex.Match( rawLine );

			if( tokenMatch.Success == false )
			{
				throw new ArgumentException( "Invalid syntax.", "rawLine" );
			}

			var name = tokenMatch.Groups["name"].Value;
			var value = tokenMatch.Groups["value"].Value;
			var indent = tokenMatch.Groups["indent"].Value;

			// note the indentation style, and then enforce it the rest of the file (and any included files!)

			if( this.IndentationStyle == TabsOrSpaces.None )
			{
				if( indent.Contains( "\t" ) )
				{
					this.IndentationStyle = TabsOrSpaces.Tabs;
				}
				if( indent.Contains( " " ) )
				{
					this.IndentationStyle = TabsOrSpaces.Spaces;
				}
			}

			if( indent.Contains( "\t" ) && this.IndentationStyle != TabsOrSpaces.Tabs )
			{
				throw new ArgumentException( "Encountered a tab-indent while in space-indent mode.", "rawLine" );
			}

			if( indent.Contains( " " ) && this.IndentationStyle != TabsOrSpaces.Spaces )
			{
				throw new ArgumentException( "Encountered a space-indent while in tab-indent mode.", "rawLine" );
			}

			// construct an object representation of this line

			var childAnswer = new Answer
			{
				Name = String.IsNullOrEmpty( name ) ? "" : name,
				Value = String.IsNullOrEmpty( value ) ? "" : value,
				Indent = indent.Length
			};

			// track this line's parent-child relationship to the previous line(s)

			if( this.ChildAnswers.Count > 0 && // if indentation has been reduced from the previous node...
				childAnswer.Indent < this.ChildAnswers.Peek().Indent )
			{
				while( this.ChildAnswers.Count > 0 )
				{
					this.ChildAnswers.Pop(); // pop nodes until we've reached a node that matches our current indentation 

					if( childAnswer.Indent == this.ChildAnswers.Peek().Indent )
					{
						break;
					}

					if( childAnswer.Indent > this.ChildAnswers.Peek().Indent )
					{
						throw new ArgumentException( "Improperly aligned indentation.", "rawLine" );
					}
				}
			}

			// if the current indentation matches the indentation of the node at the top of the stack...
			if( this.ChildAnswers.Count > 0 && 
				childAnswer.Indent == this.ChildAnswers.Peek().Indent )
			{
				this.ChildAnswers.Pop(); // swap the node at the top of the stack with the current node (see also Push(newNode) below)
			}

			// set this node's parent node

			childAnswer.Parent = this.ChildAnswers.Count > 0 ? this.ChildAnswers.Peek() : null; 

			// create a list of child nodes

			this.ChildAnswers.Push( childAnswer );

			if( childAnswer.Parent != null )
			{
				childAnswer.Parent.Children.Add( childAnswer ); // add this node as a child of its parent node
			}
			else
			{
				this.Answers.Add( childAnswer ); // add this node as a root node
			}
		}

		public void Parse( String rawLine )
		{
			var lines = LineSplitterRegex.Split( rawLine );

			for( var i = 0; i < lines.Length; i++ )
			{
				try
				{
					ParseLine( lines[i] );
				}
				catch( ArgumentException ex )
				{
					throw new ArgumentException( "Error on line " + i, "rawLine", ex );
				}
			}
		}

		public String Serialize()
		{
			var sb = new StringBuilder();

			foreach( var answer in this.Answers )
			{
				sb.AppendLine( answer.Serialize() );
				sb.AppendLine();
			}

			return sb.ToString();
		}
	}
}
