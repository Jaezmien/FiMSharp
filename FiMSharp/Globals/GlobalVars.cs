using System.Collections.Generic;

namespace FiMSharp.GlobalVars
{
    public class Globals
    {
        public static char[] Punctuations = new char[] { '.', '!', '?', ':', ',' };

        public static string ReportStart = "Dear Princess Celestia: ";

        public static string ReportEnd = "Your faithful student, ";

        public static class Methods
        {
            public static string[] Print = { "I said ", "I sang ", "I wrote ", "I remembered ", "I would " };
            public static string[] Read = { "I heard ", "I read ", "I asked " };
            public static string Variable_Declaration = "Did you know that ";
            public static string Return = "Then you get ";
            public static string[] Variable_Replace = { "become", "became", "becomes", "is now", "now likes", "now like", "are now" };
            public static string[] Variable_Initialization = { "has", "is", "like", "likes", "was" };
            public static string Constant = "always ";

            public static string[] Paragraph_Return = { " to get ", " with " };
            public static string Paragraph_Param = " using ";

            public static string[] Variable_Boolean = {"an argument", "the argument", "the logic", "argument", "logic"};
            public static string[] Variable_Boolean_Array = {"many arguments", "many logics", "the arguments", "the logics", "arguments", "logics"};
            public static string[] Variable_Number = {"the number", "a number", "number"};
            public static string[] Variable_Number_Array = {"many numbers", "the numbers", "numbers"};
            public static string[] Variable_String = {"a phrase", "a quote", "a sentence", "a word", "characters", "letters", "phrase", "quote", "sentence", "the characters", "the letters", "the phrase", "the quote", "the sentence", "the word", "word"};
            public static string[] Variable_String_Array = {"many phrases", "many quotes", "many sentences", "many words", "phrases", "quotes", "sentences", "the phrases", "the quotes", "the sentences", "the words", "words"};
            public static string[] Variable_Character = {"a character", "a letter", "the character", "the letter", "character", "letter"};

            public static string[] Boolean_True = {"correct", "right", "true", "yes"};
            public static string[] Boolean_False = {"false", "incorrect", "no", "wrong"};

            public static string[] If_Statement = {"If ", "When "};
            public static string[] Else_Statement = {"Or else", "Otherwise"};
            public static string[] While_Statement = {"As long as ", "While "};
            public static string Switch_Statement = "In regards to ";
            public static string For_Statement = "For every ";
            
            public static string If_Statement_End = "That's what I would do";
            public static string While_Statement_End = "That's what I did";
            public static string Switch_Statement_End = While_Statement_End;
            public static string For_Statement_End = While_Statement_End;

            public static string Switch_Statement_Case = "On the ";
            public static string Switch_Statement_Case_End = " hoof...";
            public static string Switch_Statement_Default = "If all else fails";

            public static string[] Conditional_LessThanEqual = {"had no more than", "has no more than", "is no greater than", "is no more than", "is not greater than", "is not more than", "isn't greater than", "isn't more than", "was no greater than", "was no more than", "was not greater than", "was not more than", "wasn't greater than", "wasn't more than", "were no greater than", "were no more than", "were not greater than", "were not more than", "weren't greater than", "weren't more than"};
            public static string[] Conditional_GreaterThan = {"had more than", "has more than", "is greater than", "was greater than", "were greater than", "were more than", "was more than"};
            public static string[] Conditional_GreaterThanEqual = {"had no less than", "has no less than", "is no less than", "is not less than", "isn't less than", "was no less than", "was not less than", "wasn't less than", "were no less than", "were not less than", "weren't less than"};
            public static string[] Conditional_LessThan = {"had less than", "has less than", "is less than", "was less than", "were less than"};
            public static string[] Conditional_Not = {"wasn't equal to", "isn't equal to", "weren't equal to", "hadn't", "had not", "hasn't", "has not", "isn't", "is not", "wasn't", "was not", "weren't", "were not"};
            public static string[] Conditional_Equal = {"is equal to", "was equal to", "were equal to", "had", "has", "is", "was", "were"};
        }
        
        public static Dictionary<string, string> Conditionals = new Dictionary<string, string>() {
            {"Less Than Equal", "<="},
            {"Greater Than", ">"},
            {"Greater Than Equal", ">="},
            {"Less Than", "<"},
            {"Not", "!="},
            {"Equal", "=="},
        };
        
        public class ArithmeticStruct {
            public string[] Prefix { get; set; }
            public string[] PrefixInfix { get; set; }
            public string[] Infix { get; set; }
        }
        public static Dictionary<string,ArithmeticStruct> Arithmetic = new Dictionary<string, ArithmeticStruct>()
        {
            {
                "Add",
                new ArithmeticStruct() {
                    Prefix = new string[] { "add" },
                    PrefixInfix = new string[] { "and" },
                    Infix = new string[] { "added to", "plus" }
                }
            },
            {
                "Subtract",
                new ArithmeticStruct() {
                    Prefix = new string[] { "subtract", "the difference between" },
                    PrefixInfix = new string[] { "and" },
                    Infix = new string[] { "minus", "without" }
                }
            },
            {
                "Multiply",
                new ArithmeticStruct() {
                    Prefix = new string[] { "multiply", "the product of" },
                    PrefixInfix = new string[] { "by", "and" },
                    Infix = new string[] { "multiplied with", "times" }
                }
            },
            {
                "Divide",
                new ArithmeticStruct() {
                    Prefix = new string[] { "divide" },
                    PrefixInfix = new string[] { "by", "and" },
                    Infix = new string[] { "divided by", "over" }
                }
            },
            {
                "Remainder",
                new ArithmeticStruct() {
                    Prefix = new string[] { "the remainder of" },
                    PrefixInfix = new string[] { "and" },
                    Infix = new string[] { "modulo", "mod" }
                }
            },
        };

        public static string[] Keywords = {
            "correct", "right", "true", "yes",
            "false", "incorrect", "no", "wrong",
            "That's what I would do",
            "That's what I did",
            "If all else fails",
            "Or else", "Otherwise",
            "On the",
            "If",
            "When",
            "In regards to",
            "Dear",
            "I learned",
            "Did you know that",
            "Your faithful student",
            "That's all about",
            "Remember when I wrote about",
            "has",
            "is",
            "like",
            "was",
            "always has",
            "always is",
            "always like",
            "always likes",
            "always was",
            "and",
            "using",
            "I did this while",
            "I did this as long as",
            "That's what I did",
            "Here's what I did",
            "I said",
            "I sang",
            "I wrote",
            "I asked",
            "I heard",
            "I read",
            "Then you get",
            "I remembered",
            "I would",
            "argument",
            "logic",
            "logics",
            "number",
            "character",
            "letter",
            "numbers",
            "phrases",
            "quotes",
            "sentences",
            "words",
            "phrase",
            "quote",
            "sentence",
            "word",
            "are now",
            "become",
            "became",
            "becomes",
            "is now",
            "now like",
            "now likes",
            "using",
            "to get",
            "with",
            "had",
            "has",
            "were",
            "hadn't",
            "had not",
            "hasn't",
            "has not",
            "isn't",
            "is not",
            "wasn't",
            "was not",
            "weren't",
            "were not",
            "had more than",
            "has more than",
            "is greater than",
            "was greater than",
            "were greater than",
            "were more than",
            "had no less than",
            "has no less than",
            "is no less than",
            "is not less than",
            "isn't less than",
            "was no less than",
            "was not less than",
            "wasn't less than",
            "were no less than",
            "were not less than",
            "weren't less than",
            "had less than",
            "has less than",
            "is less than",
            "was less than",
            "were less than",
            "had no more than",
            "has no more than",
            "is no greater than",
            "is no more than",
            "is not greater than",
            "is not more than",
            "isn't greater than",
            "isn't more than",
            "was no greater than",
            "was no more than",
            "was not greater than",
            "was not more than",
            "wasn't greater than",
            "wasn't more than",
            "were no greater than",
            "were no more than",
            "were not greater than",
            "were not more than",
            "weren't greater than",
            "weren't more than",
            "is equal to",
            "was equal to",
            "were equal to",
            "wasn't equal to",
            "isn't equal to",
            "weren't equal to",
            "As long as",
            "While",
            "There was one more",
            "got one more",
            "got one less",
            "There was one less",
            "add",
            "added to",
            "plus",
            "subtract",
            "the difference between",
            "minus",
            "without",
            "multiply",
            "the product of",
            "by",
            "and",
            "multiply with",
            "times",
            "divide",
            "divided by",
            "over",
            "of",
        };
    }
}
