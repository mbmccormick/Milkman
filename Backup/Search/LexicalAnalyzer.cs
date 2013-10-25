using System;
using System.Collections.Generic;
using System.Text;

namespace IronCow.Search
{
    /// <summary>
    /// A class that parses an RTM search query into an AST. 
    /// </summary>
    /// <example>
    /// <code>
    /// var lexer = new LexicalAnalyzer();
    /// var tokens = lexer.Tokenize("dueBefore:\"1 week\" AND tag:@home");
    /// var nodes = lexer.BuildAst(tokens);
    /// </code>
    /// </example>
    /// <remarks>
    /// This implementation is a ad-hoc hack until I can get something better
    /// like ANTLR to work with this kind of syntax. It shouldn't matter though
    /// as search queries tend to be very small, and the only thing to scale
    /// up is the number of search operators.
    /// </remarks>
    public class LexicalAnalyzer
    {
        [Flags]
        private enum StateFlags
        {
            None = 0,
            IsInsideQuotes = (1 << 0),
            IsInsideParenthesis = (1 << 1)
        }

        public LexicalAnalyzer()
        {
        }

        /// <summary>
        /// Create tokens out of a raw search query.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public TokenCollection Tokenize(string input)
        {
            var tokens = new TokenCollection();

            if (!string.IsNullOrEmpty(input))
            {
                StateFlags state = StateFlags.None;
                string[] words = input.Split(' ', '\t');
                for (int i = 0; i < words.Length; i++)
                {
                    string word = words[i];

                    int addClosingParen = 0;
                    bool addClosingQuote = false;

                    while (word[0] == '(')
                    {
                        tokens.Add(new Token(TokenType.ParenthesisOpen));
                        word = word.Substring(1);
                    }
                    if (word.Length == 0)
                        continue;
                    if (word[0] == '"')
                    {
                        if ((state & StateFlags.IsInsideQuotes) != 0)
                            throw new Exception();
                        tokens.Add(new Token(TokenType.Quote));
                        state |= StateFlags.IsInsideQuotes;
                        word = word.Substring(1);
                    }
                    if (word.Length == 0)
                        continue;

                    while (word[word.Length - 1] == ')')
                    {
                        ++addClosingParen;
                        word = word.Substring(0, word.Length - 1);
                    }
                    if (word.Length == 0)
                    {
                        while (addClosingParen-- > 0)
                            tokens.Add(new Token(TokenType.ParenthesisClose));
                        continue;
                    }
                    if (word[word.Length - 1] == '"')
                    {
                        addClosingQuote = true;
                        word = word.Substring(0, word.Length - 1);
                    }
                    if (word.Length == 0)
                    {
                        if (addClosingQuote)
                            tokens.Add(new Token(TokenType.Quote));
                        while (addClosingParen-- > 0)
                            tokens.Add(new Token(TokenType.ParenthesisClose));
                    }

                    int semiColonIndex = word.IndexOf(':');
                    if (semiColonIndex < 0 || (state & StateFlags.IsInsideQuotes) != 0)
                    {
                        if ((state & StateFlags.IsInsideQuotes) == 0 && word.Equals("AND", StringComparison.OrdinalIgnoreCase))
                            tokens.Add(new Token(TokenType.BooleanAnd));
                        else if ((state & StateFlags.IsInsideQuotes) == 0 && word.Equals("OR", StringComparison.OrdinalIgnoreCase))
                            tokens.Add(new Token(TokenType.BooleanOr));
                        else if ((state & StateFlags.IsInsideQuotes) == 0 && word.Equals("NOT", StringComparison.OrdinalIgnoreCase))
                            tokens.Add(new Token(TokenType.UnaryNot));
                        else
                            tokens.Add(new Token(word));
                    }
                    else
                    {
                        string operatorName = word.Substring(0, semiColonIndex);
                        tokens.Add(new Token(TokenType.Operator, operatorName));

                        string operatorArgumentStart = word.Substring(semiColonIndex + 1);
                        if (operatorArgumentStart[0] == '"')
                        {
                            tokens.Add(new Token(TokenType.Quote));
                            state |= StateFlags.IsInsideQuotes;
                            operatorArgumentStart = operatorArgumentStart.Substring(1);
                        }
                        tokens.Add(new Token(operatorArgumentStart));
                    }

                    if (addClosingQuote)
                    {
                        if ((state & StateFlags.IsInsideQuotes) == 0)
                            throw new Exception();
                        tokens.Add(new Token(TokenType.Quote));
                        state &= ~StateFlags.IsInsideQuotes;
                    }
                    while (addClosingParen-- > 0)
                        tokens.Add(new Token(TokenType.ParenthesisClose));
                }
            }

            return BuildSentences(tokens);
        }

        private TokenCollection BuildSentences(TokenCollection tokens)
        {
            var compressedTokens = new TokenCollection();
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (token.Type == TokenType.Quote)
                {
                    StringBuilder sentence = new StringBuilder();
                    for (int j = i + 1; j < tokens.Count; j++)
                    {
                        if (tokens[j].Type == TokenType.Quote)
                        {
                            i = j;  // Skip to after the quote.
                            break;
                        }
                        if (tokens[j].Type != TokenType.Word)
                            throw new Exception();
                        if (j > i + 1)
                            sentence.Append(" ");
                        sentence.Append(tokens[j].Text);
                    }
                    compressedTokens.Add(new Token(sentence.ToString()));
                }
                else
                {
                    compressedTokens.Add(token);
                }
            }
            return compressedTokens;
        }

        /// <summary>
        /// Transform a compressed array of tokens into a AST.
        /// </summary>
        /// <param name="tokens"></param>
        /// <returns></returns>
        public GroupNode BuildAst(IList<Token> tokens)
        {
            var context = new NodeContext();
            var root = new ParenthesisGroupNode();
            context.PushParent(root);
            BuildGroups(tokens, context);
            if (context.ParentCount > 1)
                throw new Exception();
            if (context.LastParent != root)
                throw new Exception();
            BuildOperators(root);
            BuildUnaryBranches(root);
            BuildBooleanBranches(root);
            return root;
        }

        private void BuildGroups(IList<Token> tokens, NodeContext context)
        {
            for (int i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];

                switch (token.Type)
                {
                    case TokenType.Word:
                        {
                            var node = new TermNode(token.Text);
                            context.AddChild(node);
                        }
                        break;
                    case TokenType.BooleanAnd:
                        {
                            var node = new BooleanNode(BooleanType.And);
                            context.AddChild(node);
                        }
                        break;
                    case TokenType.BooleanOr:
                        {
                            var node = new BooleanNode(BooleanType.Or);
                            context.AddChild(node);
                        }
                        break;
                    case TokenType.Operator:
                        {
                            var node = new OperatorNode(token.Text);
                            context.AddChild(node);
                        }
                        break;
                    case TokenType.ParenthesisOpen:
                        {
                            var group = new ParenthesisGroupNode();
                            context.PushParent(group);
                        }
                        break;
                    case TokenType.ParenthesisClose:
                        {
                            context.PopParent();
                        }
                        break;
                    case TokenType.Quote:
                        // Quotes should have been removed by BuildSentences().
                        throw new Exception();
                    case TokenType.UnaryNot:
                        {
                            var node = new UnaryNode(UnaryType.Not);
                            context.AddChild(node);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        private void BuildOperators(GroupNode root)
        {
            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is OperatorNode)
                {
                    if (i == root.Count - 1)
                        throw new Exception();
                    if (!(root[i + 1] is TermNode))
                        throw new Exception();
                    var operatorNode = (OperatorNode)root[i];
                    var termNode = (TermNode)root[i + 1];
                    operatorNode.Argument = termNode.Term;
                    if (!root.Remove(termNode))
                        throw new Exception();
                }
                else if (root[i] is GroupNode)
                {
                    var parenNode = (GroupNode)root[i];
                    BuildOperators(parenNode);
                }
            }
        }

        private void BuildUnaryBranches(ParenthesisGroupNode root)
        {
            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is UnaryNode)
                {
                    if (i == root.Count - 1)
                        throw new Exception();
                    var unaryNode = (UnaryNode)root[i];
                    var childNode = root[i + 1];
                    unaryNode.Child = childNode;

                    if (childNode is ParenthesisGroupNode)
                    {
                        var parenNode = (ParenthesisGroupNode)childNode;
                        BuildUnaryBranches(parenNode);
                    }
                }
                else if (root[i] is ParenthesisGroupNode)
                {
                    var parenNode = (ParenthesisGroupNode)root[i];
                    BuildUnaryBranches(parenNode);
                }
            }
        }

        private void BuildBooleanBranches(ParenthesisGroupNode root)
        {
            for (int i = 0; i < root.Count; i++)
            {
                if (root[i] is BooleanNode)
                {
                    if (i == 0 || i == root.Count - 1)
                        throw new Exception();
                    var booleanNode = (BooleanNode)root[i];
                    var leftNode = root[i - 1];
                    var rightNode = root[i + 1];
                    booleanNode.Add(leftNode);
                    booleanNode.Add(rightNode);
                    --i; // make sure that "i++" will go back to the same value of "i",
                    // which makes us end up on the node after "rightNode".

                    if (rightNode is ParenthesisGroupNode)
                    {
                        var parenNode = (ParenthesisGroupNode)rightNode;
                        BuildBooleanBranches(parenNode);
                    }
                }
                else if (root[i] is ParenthesisGroupNode)
                {
                    var parenNode = (ParenthesisGroupNode)root[i];
                    BuildBooleanBranches(parenNode);
                }
            }
        }
    }
}
