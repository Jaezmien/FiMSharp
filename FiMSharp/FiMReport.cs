using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using FiMSharp.Core;
using FiMSharp.GlobalVars;

namespace FiMSharp
{
    public class FiMException : Exception {
        public FiMException(){}
        public FiMException(string message) : base("[FiMException] " + message) {}
        public FiMException(string message, Exception inner) : base("[FiMException] " + message, inner) {}

    }
    public class FiMReport
    {
        public FiMReport(string[] lines)
        {
            bool is_in_report = false;
            bool is_in_paragraph = false;

            string _paragraphName = "";
            int _paragraphIndex = -1;
            VariableTypes _paragraphReturn = VariableTypes.UNDEFINED;
            List<(string, VariableTypes)> _paragraphParameters = new List<(string, VariableTypes)>();

            int scopeCount = 0;
            List<int> blacklist = new List<int>();
            for( int i = 0; i < lines.Length; i++ ) {
                if( blacklist.Contains(i) ) continue;

                string line = lines[i];
                string _line = line;
                line = FiMMethods.RemoveStringParentheses( line ).TrimStart();

                if( line.Trim().Length > 0 ) {

                    try {                    
                    if( !FiMMethods.IsComment(line) ) {
                        if( !Globals.Punctuations.Any(x => line[line.Length-1] == x) ) {
                            throw new FiMException(
                                FiMMethods.CreateExceptionString( "Line doesn't end with a punctuation.", line, i )
                            );
                        }
                        line = line.Substring(0, line.Length-1);
                    }

                    if( !is_in_report && line.StartsWith(Globals.ReportStart) )
                    {
                        
                        this.ReportName = line.Substring(Globals.ReportStart.Length);
                        is_in_report = true;
                        continue;
                    }
                    else if( is_in_report && line.StartsWith(Globals.ReportEnd) )
                    {
                        this.StudentName = line.Substring(Globals.ReportEnd.Length);
                        is_in_report = false;
                        continue;
                    }

                    if( is_in_report ) {

                        if( !is_in_paragraph )
                        {
                            if( Regex.IsMatch(line, @"(Today )?I learned ") ) {
                                bool _isMain = false;

                                if( line.StartsWith("Today ") ) {
                                    if( !string.IsNullOrEmpty(this._MainParagraph) ) {
                                        throw new Exception(
                                            FiMMethods.CreateExceptionString("Only one main paragraph expected", line, i+1)
                                        );
                                    }
                                    line = line.Substring( "Today ".Length );
                                    _isMain = true;
                                }

                                line = line.Substring( "I learned ".Length );

                                // Paragraph Return
                                {
                                    if( Globals.Methods.Paragraph_Return.Any( x => line.Contains(x) ) ) {
                                        string keyword = Globals.Methods.Paragraph_Return.Where( x => line.Contains(x) ).FirstOrDefault();
                                        string[] split = line.Split( new string[] { keyword }, StringSplitOptions.None );
                                        _paragraphReturn = FiMMethods.GetVariableTypeFromDeclaration(split[1], out string type_keyword);
                                        line = split[0].Trim() + " " + split[1].Substring( Math.Min( split[1].Length, type_keyword.Length+1 ) ).Trim();
                                        line = line.Trim();
                                    }
                                }
                                // Paragraph Parameter
                                {
                                    if( line.Contains( Globals.Methods.Paragraph_Param ) ) {
                                        string[] split = line.Split( new string[] { Globals.Methods.Paragraph_Param }, StringSplitOptions.None );

                                        foreach( string param in split[1].Split( new string[] { " and " }, StringSplitOptions.None ) ) {
                                            VariableTypes _type = FiMMethods.GetVariableTypeFromDeclaration( param, out string keyword );
                                            _paragraphParameters.Add( ( param.Substring(keyword.Length + 1), _type ) );
                                        }

                                        line = split[0];
                                    }
                                }

                                _paragraphName = line;
                                _paragraphIndex = i;

                                if( _isMain ) this._MainParagraph = line;
                                is_in_paragraph = true;
                                continue;
                            }
                            // Global variables
                            var tokenize_result = Tokenizer.FiMTokenizer.TokenizeString( _line );
                            if( tokenize_result.Item1 != TokenTypes.CREATE_VARIABLE )
                                throw new FiMException(
                                    FiMMethods.CreateExceptionString("Expected only global variable creation outside of paragraphs", line, i+1)
                                );
                            
                            try {
                                var new_variable = FiMMethods.VariableFromTokenizer( this, this.Variables, tokenize_result.Item2 );
                                this.Variables.Add( new_variable.Item1, new_variable.Item2 );
                            }
                            catch (FiMException ex) {
                                throw new FiMException( ex.Message );
                            }

                        }
                        else
                        {
                            if( line.StartsWith("That's all about " + _paragraphName ) ) {
                                if( scopeCount != 0 )
                                    throw new FiMException( $"Paragraph { _paragraphName } has scoping error!" );

                                FiMParagraph paragraph = new FiMParagraph(
                                    this,
                                    _paragraphName,
                                    (_paragraphIndex + 1, i - 1),
                                    _paragraphName == this._MainParagraph,
                                    new List<(string, VariableTypes)>(_paragraphParameters),
                                    _paragraphReturn
                                );
                                this.Paragraphs.Add( _paragraphName, paragraph );

                                _paragraphIndex = -1;
                                _paragraphName = "";
                                _paragraphParameters.Clear();
                                _paragraphReturn = VariableTypes.UNDEFINED;
                                is_in_paragraph = false;
                                continue;
                            }

                            bool ignore = false;
                            if( Extension.IsStatementStart(_line.TrimStart(), out string keyword, out FiMStatementTypes statement_type) ) {
                                line = Tokenizer.FiMTokenizer.SimpleSanitize( _line.TrimStart() );
                                ignore = true;

                                if( statement_type == FiMStatementTypes.If ) {
                                    int _i = i+1; // Sub-index
                                    int _s = 0; // Scope
                                    
                                    var ifStatement = new FiMIfStatement();

                                    // Keep track
                                    string currStatement;
                                    (int, int) currLines = (i+1, i);

                                    string TokenizeStatement( string l ) {
                                        string s = l;
                                        if(s.EndsWith(" then") )
                                            s = s.Substring( 0, s.Length - " then".Length );
                                        return s.Trim();
                                    }
                                    currStatement = TokenizeStatement( line.Substring( keyword.Length ) );                                    

                                    while( true ) {
                                        if( _i >= lines.Length )
                                            throw new FiMException("Scope reached EOF");

                                        string l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart() );
                                        
                                        if( Extension.IsStatementStart(l, FiMStatementTypes.If ) ) {
                                            if( _s == 0 && ifStatement.HasElse ) throw new FiMException("Expected else to be end of if-statement");
                                            else {
                                                currLines.Item2++;
                                                _s++;
                                            }
                                        }
                                        else if( Extension.IsStatementStart(l, FiMStatementTypes.ElseIf) ) {
                                            if( _s == 0 ) {
                                                if( ifStatement.HasElse ) throw new FiMException("Expected else to be end of if-statement");
                                                blacklist.Add( _i );
                                                ifStatement.Conditions.Add( ( currStatement, currLines ) );
                                                Extension.IsStatementStart(l, out string k, out FiMStatementTypes _);
                                                currStatement = TokenizeStatement( l.Substring(k.Length) );
                                                currLines = ( _i+1, _i );
                                            }
                                            else {
                                                currLines.Item2++;
                                                _s++;
                                            }
                                        }
                                        else if( Extension.IsStatementStart(l, FiMStatementTypes.Else) ) {
                                            if( _s == 0 ) {
                                                blacklist.Add( _i );
                                                ifStatement.HasElse = true;
                                                ifStatement.Conditions.Add( ( currStatement, currLines ) );
                                                //currStatement = ("", "", "");
                                                currStatement = "";
                                                currLines = ( _i+1, _i );
                                            }
                                        }
                                        else if( Extension.IsStatementEnd(l, FiMStatementTypes.If) ) {
                                            if( _s > 0 ) {
                                                currLines.Item2++;
                                                _s--;
                                            }
                                            else {
                                                blacklist.Add( _i );
                                                ifStatement.Conditions.Add( ( currStatement, currLines ) );
                                                break;
                                            }
                                        }
                                        else {
                                            currLines.Item2++;
                                        }

                                        _i++;
                                    }

                                    this.Lines.Add(
                                        i,
                                        (line, TokenTypes.IF_STATEMENT, ifStatement)
                                    );
                                }

                                else if( statement_type == FiMStatementTypes.While ) {

                                    int _i = i+1;
                                    int _s = 0;

                                    FiMWhileStatement whileStatement = new FiMWhileStatement
                                    {
                                        Lines = (i + 1, i),
                                        Condition = line.Substring(keyword.Length)
                                    };
                                    if ( whileStatement.Condition.EndsWith(" then") )
                                        whileStatement.Condition = whileStatement.Condition.Substring( 0, whileStatement.Condition.Length - " then".Length );

                                    while( true ) {
                                        if( _i >= lines.Length )
                                            throw new FiMException("Scope reached EOF");

                                        string l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart() );

                                        if( Extension.IsStatementStart(l, FiMStatementTypes.While ) ) {
                                            _s++;
                                            whileStatement.Lines.Item2++;
                                        }
                                        else if( Extension.IsStatementEnd(l, FiMStatementTypes.While ) ) {
                                            if( _s > 0 ) {
                                                whileStatement.Lines.Item2++;
                                                _s--;
                                            }
                                            else {
                                                blacklist.Add( _i );
                                                break;
                                            }
                                        }
                                        else {
                                            whileStatement.Lines.Item2++;
                                        }

                                        _i++;
                                    }

                                    this.Lines.Add(
                                        i,
                                        (line, TokenTypes.WHILE_STATEMENT, whileStatement)
                                    );

                                }

                                else if( statement_type == FiMStatementTypes.For ) {

                                    int _i = i+1;

                                    {
                                        string l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[i].TrimStart(), out var _, false );
                                        if(!l.EndsWith("...")) throw new Exception("Missing elipsis");
                                        line = l.Substring(0, l.Length - "...".Length);
                                    }

                                    line = line.Substring( keyword.Length );
                                    if( line.EndsWith(" then" ) )
                                        line = line.Substring( 0, line.Length - " then".Length );

                                    void GrabStatementLines( FiMForStatement s ) {
                                        int _s = 0;
                                        s.Lines = (_i,_i-1);
                                        while( true ) {
                                            if( _i >= lines.Length )
                                                throw new FiMException("Scope reached EOF");

                                            string l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart() );

                                            if( Extension.IsStatementStart(l, FiMStatementTypes.For ) ) {
                                                _s++;
                                            }
                                            else if( Extension.IsStatementEnd(l, FiMStatementTypes.For ) ) {
                                                if( _s > 0 ) _s--;
                                                else {
                                                    blacklist.Add( _i );
                                                    break;
                                                }
                                            }
                                            else {
                                                s.Lines.Item2++;
                                            }

                                            _i++;
                                        }
                                    }

                                    if( line.Contains(" from ") && line.Contains(" to ") ) {
                                        var forStatement = new FiMForToStatement();
                                        GrabStatementLines( forStatement );

                                        string[] s = line.Split(new string[] { " from " }, StringSplitOptions.None );

                                        var type = FiMMethods.GetVariableTypeFromDeclaration( s[0], out string k );
                                        if( type != VariableTypes.INTEGER ) throw new FiMException("For loop element must be an number type");
                                        forStatement.Element = ( s[0].Substring(k.Length+1), type );

                                        string[] r = s[1].Split(new string[] { " to " }, StringSplitOptions.None );
                                        forStatement.Range = (r[0], r[1]);

                                        this.Lines.Add(
                                            i,
                                            (line, TokenTypes.FOR_TO_STATEMENT, forStatement)
                                        );
                                    }
                                    else if ( line.Contains(" in ") ) {
                                        var forStatement = new FiMForInStatement();
                                        GrabStatementLines( forStatement );

                                        string[] s = line.Split(new string[] { " in " }, StringSplitOptions.None );

                                        var type = FiMMethods.GetVariableTypeFromDeclaration( s[0], out string k );
                                        forStatement.Element = ( s[0].Substring(k.Length+1), type );

                                        forStatement.Variable = s[1];

                                        this.Lines.Add(
                                            i,
                                            (line, TokenTypes.FOR_IN_STATEMENT, forStatement)
                                        );
                                    }
                                    else {
                                        throw new FiMException("Invalid for loop type");
                                    }

                                }

                                else if( statement_type == FiMStatementTypes.Switch ) {
                                    int _i = i+1;
                                    int _s = 0;

                                    FiMSwitchStatement switchStatement = new FiMSwitchStatement
                                    {
                                        Switch = line.Substring(keyword.Length)
                                    };

                                    string currStatement = "";
                                    (int, int) currLines = (-1, -1);
                                    bool isDefault = false;

                                    void InsertPrevStatement() {
                                        if( currStatement == "" && isDefault ) {
                                            if( switchStatement.HasDefault() )
                                                throw new Exception("Multiple defaults in a switch statement");
                                            switchStatement.Default = currLines;
                                        }
                                        else if ( currStatement != "" && !isDefault ) {
                                            switchStatement.Case.Add( currStatement, currLines );
                                        }

                                        currStatement = "";
                                        currLines = (-1, -1);
                                        isDefault = false;
                                    }

                                    while( true ) {
                                        if( _i >= lines.Length )
                                            throw new FiMException("Scope reached EOF");

                                        string l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart() );
                                        
                                        // Switch start
                                        if( Extension.IsStatementStart(l, FiMStatementTypes.Switch) ) {
                                            _s++;
                                        }
                                        // Case start
                                        else if( l.StartsWith( Globals.Methods.Switch_Statement_Case ) && _s==0 ) {
                                            l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart(), out var _, false );
                                            if( l.EndsWith( Globals.Methods.Switch_Statement_Case_End ) ) {
                                                l = l.Substring( Globals.Methods.Switch_Statement_Case.Length );
                                                l = l.Substring( 0, l.Length - Globals.Methods.Switch_Statement_Case_End.Length );
                                                
                                                InsertPrevStatement();

                                                if( l.Substring(l.Length-2) == FiMMethods.GetOrdinal( int.Parse(l.Substring(0,l.Length-2)) ) ) {
                                                    l = l.Substring(0,l.Length-2);
                                                    currStatement = l;
                                                } else {
                                                    currStatement = l;
                                                }
                                                currLines = (_i+1, _i);
                                                blacklist.Add( _i );
                                            }
                                        }
                                        // Case start (default)
                                        else if ( l.StartsWith( Globals.Methods.Switch_Statement_Default ) && _s==0 ) {
                                            l = l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart(), out var _, false );
                                            if( l.EndsWith("...") ) {
                                                InsertPrevStatement();
                                                
                                                isDefault = true;
                                                currLines = (_i+1, _i);

                                                blacklist.Add( _i );
                                            }
                                        }
                                        // Swtich end
                                        else if ( l.Equals( Globals.Methods.Switch_Statement_End ) ) {
                                            if( _s > 0 ) _s--;
                                            else {
                                                InsertPrevStatement();

                                                switchStatement.EndIndex = _i;
                                                blacklist.Add( _i );
                                                break;
                                            }
                                        }
                                        else {
                                            currLines.Item2++;
                                        }

                                        _i++;
                                    }
                                    
                                    this.Lines.Add(
                                        i,
                                        (line, TokenTypes.SWITCH_STATEMENT, switchStatement)
                                    );
                                }
                            }

                            if( !ignore )
                            {
                                var tokenize_result = Tokenizer.FiMTokenizer.TokenizeString( _line );
                                this.Lines.Add(
                                    i,
                                    (line, tokenize_result.Item1, tokenize_result.Item2)
                                );
                            }

                        }

                    }
                    } catch( FiMException ex ) {
                        throw new FiMException(
                            FiMMethods.CreateExceptionString( ex.Message, _line.TrimStart(), i + 1 )
                        );
                    }

                } else {
                    continue; // Empty
                }
            }

            if( is_in_report )
                throw new FiMException( "EOF not found" );
            if( is_in_paragraph )
                throw new FiMException( "EOM not found" );

            OriginalLines = lines;
        }

        public readonly Dictionary<int, (string, TokenTypes, object)> Lines = new Dictionary<int, (string, TokenTypes, object)>();
        public readonly Dictionary<string, FiMVariable> Variables = new Dictionary<string, FiMVariable>();
        public readonly Dictionary<string, FiMParagraph> Paragraphs = new Dictionary<string, FiMParagraph>();

        public readonly string[] OriginalLines = new string[] {};
        private readonly string _MainParagraph;
        public FiMParagraph MainParagraph {
            get {
                if( !string.IsNullOrEmpty( _MainParagraph ) ) return Paragraphs[ _MainParagraph ];
                return null;
            }
        }
        public readonly string StudentName;
        public readonly string ReportName;
    }
}