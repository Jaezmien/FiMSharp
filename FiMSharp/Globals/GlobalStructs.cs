using System.Collections.Generic;
using FiMSharp.GlobalVars;
using FiMSharp.Core;

namespace FiMSharp.GlobalStructs
{
    public struct FiMLine {
        public int Start;
        public int End;

        public FiMLine(int _s, int _e) {
            Start = _s;
            End = _e;
        }
    }
    public struct FiMLineToken {
        public string Line;
        public TokenTypes Token;
        public object Arguments;

        public FiMLineToken( string _l, TokenTypes _t, object _a ) {
            Line = _l;
            Token = _t;
            Arguments = _a;
        }
        public FiMLineToken( string _l, FiMTokenizerResult _t ) {
            Line = _l;
            Token = _t.Token;
            Arguments = _t.Arguments;
        }
    }
    public struct FiMTokenizerResult {
        public TokenTypes Token;
        public object Arguments;

        public FiMTokenizerResult( TokenTypes _t, object _a ) {
            Token = _t;
            Arguments = _a;
        }
    }

    public struct FiMParagraphParameter {
        public string Name;
        public VariableTypes Type;

        public FiMParagraphParameter( string _n, VariableTypes _t ) {
            Name = _n;
            Type = _t;
        }
    }

    public struct FiMVariableStruct {
        public object Value;
        public VariableTypes Type;

        public FiMVariableStruct(object _v, VariableTypes _t ) {
            Value = _v;
            Type = _t;
        }
        public void Deconstruct(out object _v, out VariableTypes _t) {
            _v = Value;
            _t = Type;
        }
    }
    public struct FiMStatementVariableStruct {
        public string Name;
        public VariableTypes Type;

    }
    public struct FiMIfCondition {
        public string Condition;
        public FiMLine Lines;

        public FiMIfCondition( string _c, FiMLine _l ) {
            Condition = _c;
            Lines = _l;
        }
    }
    public struct FiMForRange {
        public string From;
        public string To;
        public string By;
    }
    public struct FiMConditionalKeyword {
        public string Keyword;
        public string Sign;

        public FiMConditionalKeyword(string _kw = "", string _s = "") {
            Keyword = _kw;
            Sign = _s;
        }
        public void Deconstruct(out string _kw, out string _s) {
            _kw = Keyword;
            _s = Sign;
        }
    }
    public struct FiMArithmethicResult {
        public bool IsPrefix;
        public string Type;

        public FiMArithmethicResult( bool _i, string _t ) {
            IsPrefix = _i;
            Type = _t;
        }
    }
    public struct FiMForInStruct {
        public FiMVariableStruct Returned;
        public Dictionary<string, FiMVariable> Changed;

        public FiMForInStruct( FiMVariableStruct _r, Dictionary<string, FiMVariable> _c ) {
            Returned = _r;
            Changed = _c;
        }
    }
    public struct FiMVariableTokenizer {
        public string Name;
        public FiMVariable Variable;

        public FiMVariableTokenizer( string _n, FiMVariable _v ) {
            Name = _n;
            Variable = _v;
        }
    }
    public struct FiMParsedArray {
        public int Index;
        public FiMVariable Variable;

        public FiMParsedArray( int _i, FiMVariable _v ) {
            Index = _i;
            Variable = _v;
        }
        public void Deconstruct(out int _i, out FiMVariable _v) {
            _i = Index;
            _v = Variable;
        }
    }
    public struct FiMArrayMatch1 { 
        public string Result;
        public string Name;
        public int Index;
        public string Keyword;

        public FiMArrayMatch1(string _r, string _n , int _i, string _kw ) {
            Result = _r;
            Name = _n;
            Index = _i;
            Keyword = _kw;
        }
        public void Deconstruct(out string _r, out string _n, out int _i, out string _kw) {
            _r = Result;
            _n = Name;
            _i = Index;
            _kw = Keyword;
        }
    }
    public struct FiMArrayMatch2 { 
        public string Result;
        public string Name;
        public string Index;
        public string Keyword;

        public FiMArrayMatch2(string _r, string _n , string _i, string _kw ) {
            Result = _r;
            Name = _n;
            Index = _i;
            Keyword = _kw;
        }

        public void Deconstruct(out string _r, out string _n, out string _i, out string _kw) {
            _r = Result;
            _n = Name;
            _i = Index;
            _kw = Keyword;
        }
    }
}