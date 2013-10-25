using System;

namespace IronCow.Search
{
    public enum TokenType
    {
        Word,
        BooleanAnd,
        BooleanOr,
        UnaryNot,
        Operator,
        ParenthesisOpen,
        ParenthesisClose,
        Quote
    }

    public class Token
    {
        public TokenType Type { get; set; }
        public string Text { get; set; }

        public Token(TokenType type)
        {
            Type = type;
        }

        public Token(string text)
        {
            Type = TokenType.Word;
            Text = text;
        }

        public Token(TokenType type, string text)
        {
            Type = type;
            Text = text;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case TokenType.Word:
                    return "WORD(" + Text + ")";
                case TokenType.BooleanAnd:
                    return "BOOL_AND";
                case TokenType.BooleanOr:
                    return "BOOL_OR";
                case TokenType.Operator:
                    return "OP(" + Text + ")";
                case TokenType.ParenthesisOpen:
                    return "PAREN_OPEN";
                case TokenType.ParenthesisClose:
                    return "PAREN_CLOSE";
                case TokenType.Quote:
                    return "QUOTE";
                case TokenType.UnaryNot:
                    return "UNARY_NOT";
                default:
                    throw new NotImplementedException();
            }   
        }
    }
}
