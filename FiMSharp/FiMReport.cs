using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using FiMSharp.Core;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp
{
    public class FiMException : Exception {
        public FiMException(){}
        public FiMException(string message) : base(message) {}
        public FiMException(string message, Exception inner) : base(message, inner) {}
    }
    public class FiMPartialException: Exception {
        public FiMPartialException(){}
        public FiMPartialException(string message) : base( message) {}
        public FiMPartialException(string message, Exception inner) : base(message, inner) {}
    }

    /// <summary>
    /// A FiM++ instance.
    /// </summary>
    public class FiMReport
    {
        /// <summary>
        /// Creates a FiMReport instance.
        /// </summary>
        /// <param name="lines">The complete lines of the FiM++ report.</param>
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
                        if( !Globals.Punctuations.Any(x => line[line.Length-1] == x) )
                            throw FiMError.CreatePartial( FiMErrorType.MISSING_PUNCTUATION);
                        line = line.Substring(0, line.Length-1);
                    }

                    if( !is_in_report && line.StartsWith(Globals.ReportStart) && string.IsNullOrWhiteSpace(this.ReportName) )
                    {
                        this.ReportName = line.Substring(Globals.ReportStart.Length);
                        is_in_report = true;
                        continue;
                    }
                    else if( is_in_report && line.StartsWith(Globals.ReportEnd) && string.IsNullOrWhiteSpace(this.StudentName))
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
                                    if( !string.IsNullOrEmpty(this._MainParagraph) )
                                        throw FiMError.CreatePartial( FiMErrorType.TOO_MANY_MAIN_PARAGRAPH);
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
                                throw FiMError.CreatePartial( FiMErrorType.REPORT_VARIABLE_CREATION_ONLY );
                            
                            var new_variable = FiMMethods.VariableFromTokenizer( this, this.Variables, tokenize_result.Item2 );
                            this.Variables.Add( new_variable.Item1, new_variable.Item2 );

                        }
                        else
                        {
                            if( line.StartsWith("That's all about " + _paragraphName ) ) {
                                if( scopeCount != 0 )
                                    throw FiMError.CreatePartial( FiMErrorType.SCOPE_ERROR, _paragraphName );

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
                                            throw FiMError.Create( FiMErrorType.SCOPE_REACHED_EOF );

                                        string l = Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart() );
                                        
                                        if( Extension.IsStatementStart(l, FiMStatementTypes.If ) ) {
                                            if( _s == 0 && ifStatement.HasElse ) throw FiMError.Create( FiMErrorType.IF_STATEMENT_EXPECTED_END, l, _i );
                                            else {
                                                currLines.Item2++;
                                                _s++;
                                            }
                                        }
                                        else if( Extension.IsStatementStart(l, FiMStatementTypes.ElseIf) ) {
                                            if( _s == 0 ) {
                                                if( ifStatement.HasElse ) throw FiMError.Create( FiMErrorType.IF_STATEMENT_EXPECTED_END, l, _i );
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
                                            throw FiMError.Create( FiMErrorType.SCOPE_REACHED_EOF );

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
                                        if(!l.EndsWith("...")) throw FiMError.CreatePartial( FiMErrorType.MISSING_ELIPSIS );
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
                                                throw FiMError.Create( FiMErrorType.SCOPE_REACHED_EOF );

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
                                        if( type != VariableTypes.INTEGER ) throw FiMError.CreatePartial( FiMErrorType.FORTO_ELEMENT_NUMBER_ONLY );
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
                                        throw FiMError.CreatePartial( FiMErrorType.INVALID_FOR_LOOP_TYPE );
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
                                                throw FiMError.Create( FiMErrorType.SWITCH_MULTIPLE_DEFAULT, Tokenizer.FiMTokenizer.SimpleSanitize( lines[_i].TrimStart() ), _i );
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
                                            throw FiMError.Create( FiMErrorType.SCOPE_REACHED_EOF );

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
                    else
                    {
                        if (!FiMMethods.IsComment(line) && !string.IsNullOrWhiteSpace(this.ReportName))
                            throw FiMError.CreatePartial(FiMErrorType.EXPECTED_END_OF_REPORT);
                    }
                    }
                    catch( FiMPartialException partial ) {
                        throw FiMError.Create( partial, line, i );
                    }

                } else {
                    continue; // Empty
                }
            }

            if( is_in_report )
                throw FiMError.Create( FiMErrorType.NO_END_OF_REPORT );
            if( is_in_paragraph )
                throw FiMError.Create( FiMErrorType.NO_END_OF_PARAGRAPH, f: _paragraphName );

            if( string.IsNullOrWhiteSpace( ReportName) )
                throw FiMError.Create( FiMErrorType.REPORT_NAME_NOT_FOUND );
            if( string.IsNullOrWhiteSpace( StudentName ) )
                throw FiMError.Create( FiMErrorType.STUDENT_NAME_NOT_FOUND );

            OriginalLines = lines;
        }

        /// <summary>
        /// The tokenized lines of the report.
        /// The key of the dictionary is the line index.
        /// While the value is a tuple of the original line, the recognized token, and any miscellaneous values.
        /// </summary>
        public readonly Dictionary<int, (string, TokenTypes, object)> Lines = new Dictionary<int, (string, TokenTypes, object)>();

        /// <summary>
        /// The global variables in the report.
        /// </summary>
        public readonly Dictionary<string, FiMVariable> Variables = new Dictionary<string, FiMVariable>();

        /// <summary>
        /// The paragraphs of the report.
        /// </summary>
        public readonly Dictionary<string, FiMParagraph> Paragraphs = new Dictionary<string, FiMParagraph>();

        /// <summary>
        /// The original <c>string[]</c> of the report.
        /// </summary>
        public readonly string[] OriginalLines = new string[] {};
        private readonly string _MainParagraph;

        /// <summary>
        /// Grabs the main paragraph (if present).
        /// </summary>
        /// <value>A <c>FiMParagraph</c> or <c>null</c></value>
        public FiMParagraph MainParagraph {
            get {
                if( !string.IsNullOrEmpty( _MainParagraph ) ) return Paragraphs[ _MainParagraph ];
                return null;
            }
        }
        
        /// <summary>
        /// The name of the student writing the report.
        /// </summary>
        public readonly string StudentName;
        /// <summary>
        /// The name of the report.
        /// </summary>
        public readonly string ReportName;
    }
}