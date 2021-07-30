using System;
using System.Collections.Generic;
using System.Linq;

using FiMSharp.GlobalStructs;
using FiMSharp.GlobalVars;
using FiMSharp.Error;

namespace FiMSharp.Core
{

    public class FiMParagraph
    {

        /// <summary>
        /// The name of the paragraph.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// The lines of the paragraph.
        /// </summary>
        public readonly FiMLine Lines = new FiMLine(-1, -1);

        /// <summary>
        /// The parameters of the paragraph.
        /// The <c>string</c> is what variable name the paragraph will assign the value to.
        /// The <c>VariableTypes</c> is what variable type it is expecting.
        /// </summary>
        public readonly List<FiMParagraphParameter> Parameters;
        
        /// <summary>
        /// The paragraph variable's return type.
        /// </summary>
        public readonly VariableTypes ReturnType;
        private readonly bool IsMain;
        private readonly FiMReport report;

        /// <summary>
        /// Creates a FiMParagraph.
        /// </summary>
        /// <param name="report">The report the paragraph is in.</param>
        /// <param name="paragraph_name_">The name of the paragraph.</param>
        /// <param name="lines_">The lines of the paragraph.</param>
        /// <param name="main_">Boolean value whether or not this is the main paragraph.</param>
        /// <param name="parameters_">The parameters of the paragraph.</param>
        /// <param name="return_">The return type of the paragraph.</param>
        /// <returns></returns>
        public FiMParagraph( FiMReport report, string paragraph_name_, FiMLine lines_, bool main_, List<FiMParagraphParameter> parameters_, VariableTypes return_)
        {
            this.report = report; // Oh boy, I sure hope this is passed by reference!
            this.Name = paragraph_name_;
            this.Lines = lines_;
            this.IsMain = main_;
            this.Parameters = parameters_;
            this.ReturnType = return_;
        }

        private FiMVariableStruct Execute(int lineBegin, int lineEnd, out Dictionary<string,FiMVariable> changedVariables, Dictionary<string, FiMVariable> variables = null)
        {
            changedVariables = new Dictionary<string, FiMVariable>();
            Dictionary<string, FiMVariable> localVariables = new Dictionary<string, FiMVariable>();

            // Ain't sure if this is efficient lol
            FiMVariable GetVariable( string var_name ) {
                try {
                    if( report.Variables.ContainsKey(var_name) ) return report.Variables[ var_name ];
                    if( variables != null && variables.ContainsKey(var_name) ) return variables[ var_name ];
                    return localVariables[ var_name ];
                }
                catch {
                    throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, var_name );
                }
            }
            void SetVariable( string var_name, FiMVariable var ) {
                if( report.Variables.ContainsKey(var_name) ) report.Variables[ var_name ] = var;
                else if( variables != null && variables.ContainsKey(var_name) ) variables[ var_name ] = var;
                else {
                    if( localVariables.ContainsKey(var_name) )
                        localVariables[var_name] = var;
                    else
                        localVariables.Add( var_name, var );
                }
            }
            void SetVariableValue( string var_name, object value ) {
                GetVariable( var_name ).SetValue(value);
            }
            bool HasVariable( string var_name ) {
                return report.Variables.ContainsKey(var_name) || (variables != null && variables.ContainsKey(var_name)) || localVariables.ContainsKey(var_name);
            }
            // bool IsGlobalVariable( string var_name ) => report.Variables.ContainsKey(var_name);
            bool IsScopeVariable( string var_name ) => variables != null && variables.ContainsKey(var_name);
            // bool IsLocalVariable( string var_name ) => localVariables.ContainsKey(var_name);
            Dictionary<string,FiMVariable> CombineVariables() {
                var t = new Dictionary<string, FiMVariable>();
                foreach( var e in localVariables )
                    t.Add( e.Key, e.Value );
                if( variables != null ) {
                    foreach( var e in variables )
                        t.Add( e.Key, e.Value );
                }
                return t;
            }
            Dictionary<string,FiMVariable> CombineAllVariables() {
                var t = new Dictionary<string, FiMVariable>();
                foreach( var e in localVariables )
                    t.Add( e.Key, e.Value );
                if( variables != null ) {
                    foreach( var e in variables )
                        t.Add( e.Key, e.Value );
                }
                foreach( var e in report.Variables )
                    t.Add( e.Key, e.Value );
                return t;
            }

            int index = lineBegin;
            while( index <= lineEnd ) {
                if( report.Lines.ContainsKey(index) ) {

                    var line = report.Lines[ index ];

                    try {
                    switch( line.Token ) {

                        case TokenTypes.RUN: {
                            {
                                string _pname = (string)line.Arguments;
                                if( _pname.Contains(" using ") ) _pname = _pname.Split(new string[] {" using "}, StringSplitOptions.None)[0].Trim();

                                if( !report.Paragraphs.ContainsKey(_pname) ) throw FiMError.CreatePartial( FiMErrorType.MISSING_PARAGRAPH, _pname );
                            }
                            FiMMethods.ParseVariable( (string)line.Arguments, report, CombineAllVariables(), out VariableTypes _ );
                        }
                        break;
                        case TokenTypes.PRINT: {
                            string output = FiMMethods.SanitizeString( (string)line.Arguments, report, CombineAllVariables() );
                            this.report.ConsoleOutput.WriteLine(output);
                        }
                        break;

                        case TokenTypes.PROMPT:
                        case TokenTypes.READ: {
                            string[] arguments = (string[])line.Arguments;
                            string variable_name = arguments[0];

                            if( !HasVariable(variable_name) ) throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, variable_name );
                            FiMVariable var = GetVariable( variable_name );

                            if( arguments.Length > 1 ) {
                                string output = FiMMethods.SanitizeString( arguments[1], report, CombineAllVariables() );
                                this.report.ConsoleOutput.WriteLine(output);
                            }
                            string input = this.report.ConsoleInput.ReadLine();

                            if( var.Type == VariableTypes.STRING ) input = $"\"{input}\"";
                            else if( var.Type == VariableTypes.CHAR ) {
                                if( input.Length != 1 ) throw FiMError.CreatePartial( FiMErrorType.TOO_MANY_CHARACTERS );
                                input = $"\'{input}\'";
                            }

                            object new_value = FiMMethods.ParseVariable( input, report, CombineAllVariables(), out var input_type, fallback: VariableTypes.STRING );
                            if( input_type != var.Type ) throw FiMError.CreatePartial( FiMErrorType.UNEXPECTED_TYPE, var.Type, input_type );

                            SetVariableValue( variable_name, new_value );
                        }
                        break;
                        
                        case TokenTypes.CREATE_VARIABLE: {
                            var new_variable = FiMMethods.VariableFromTokenizer( report, CombineAllVariables(), line.Arguments );
                            SetVariable( new_variable.Name, new_variable.Variable );
                        }
                        break;
                        case TokenTypes.VARIABLE_REPLACE: {
                            List<object> args = line.Arguments as List<object>;
                            string variable_name = (string)args[0];
                            string _variable_value = (string)args[1];

                            if( !HasVariable(variable_name) ) throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, variable_name );

                            FiMVariable var = GetVariable( variable_name );
                            VariableTypes _expected_type = args.Count == 3 ? (VariableTypes)args[2] : var.Type;

                            object new_value = FiMMethods.ParseVariable( _variable_value, report, CombineAllVariables(), out VariableTypes _got_type, run_once: false, fallback: _expected_type );
                            if( _got_type != _expected_type ) {
                                if( _expected_type != VariableTypes.STRING ) throw FiMError.CreatePartial( FiMErrorType.UNEXPECTED_TYPE, _expected_type, _got_type );
                                new_value = new_value.ToString(); // :^)
                            }
                                
                            SetVariableValue( variable_name, new_value );
                            if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                        }
                        break;

                        case TokenTypes.ARRAY_MODIFY: {
                            List<object> args = line.Arguments as List<object>;
                            string variable_name = (string)args[0];
                            int array_index = (int)args[1];
                            string keyword = (string)args[2];
                            VariableTypes expected_type;
                            string value;

                            var result = FiMMethods.ParseArray( array_index, variable_name, CombineAllVariables() );

                            if( args.Count == 5 ) {
                                expected_type = (VariableTypes)args[3];
                                value = (string)args[4];
                            } else {
                                expected_type = FiMMethods.VariableTypeArraySubType( result.Variable.Type );
                                value = (string)args[3];
                            }

                            object new_value = FiMMethods.ParseVariable( value, report, CombineAllVariables(), out VariableTypes _got_type, fallback: expected_type );
                            if( _got_type != expected_type )
                                throw FiMError.CreatePartial( FiMErrorType.UNEXPECTED_TYPE, expected_type, _got_type );

                            result.Variable.SetArrayValue( result.Index, new_value );
                            if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                        }
                        break;
                        case TokenTypes.ARRAY_MODIFY2: {
                            List<object> args = line.Arguments as List<object>;
                            string variable_name = (string)args[0];
                            string variable_index = (string)args[1];
                            string keyword = (string)args[2];
                            string value = (string)args[3];

                            var result = FiMMethods.ParseArray( variable_index, variable_name, report, CombineAllVariables() );

                            VariableTypes expected_type = FiMMethods.VariableTypeArraySubType( result.Variable.Type );
                            object new_value = FiMMethods.ParseVariable( value, report, CombineAllVariables(), out VariableTypes _got_type, fallback: expected_type );
                            if( _got_type != expected_type )
                                throw FiMError.CreatePartial( FiMErrorType.UNEXPECTED_TYPE, expected_type, _got_type );

                            result.Variable.SetArrayValue( result.Index, new_value );
                            if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                        }
                        break;

                        case TokenTypes.VARIABLE_INCREMENT: {
                            string variable_name = (string)line.Arguments;
                            if( HasVariable(variable_name) ) {
                                FiMVariable variable = GetVariable( variable_name );
                                if( variable.Type != VariableTypes.INTEGER )
                                    throw FiMError.CreatePartial( FiMErrorType.INCREMENT_ONLY_NUMBERS );

                                SetVariableValue( variable_name, Convert.ToDouble(variable.GetValue().Value) + 1 );
                                if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                            } else {
                                if( FiMMethods.IsMatchArray1(variable_name, true) ) {
                                    (string _result, string _variable_name, int _variable_index, string _) = FiMMethods.MatchArray1( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, _variable_name );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToDouble(var.GetValue(i).Value)+1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else if( FiMMethods.IsMatchArray2(variable_name, true) ) {
                                    (string _result, string _variable_name, string _variable_index, string _) = FiMMethods.MatchArray2( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, _variable_name );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, report, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToDouble(var.GetValue(i).Value)+1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else {
                                    throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, variable_name );
                                }
                            }
                        }
                        break;
                        case TokenTypes.VARIABLE_DECREMENT: {
                            string variable_name = (string)line.Arguments;
                            if( HasVariable(variable_name) ) {
                                FiMVariable variable = GetVariable( variable_name );
                                if( variable.Type != VariableTypes.INTEGER )
                                    throw FiMError.CreatePartial( FiMErrorType.DECREMENT_ONLY_NUMBERS);

                                SetVariableValue( variable_name, Convert.ToDouble(variable.GetValue().Value) - 1 );
                                if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                            } else {
                                if( FiMMethods.IsMatchArray1(variable_name, true) ) {
                                    (string _result, string _variable_name, int _variable_index, string _) = FiMMethods.MatchArray1( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, _variable_name );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToDouble(var.GetValue(i).Value)-1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else if( FiMMethods.IsMatchArray2(variable_name, true) ) {
                                    (string _result, string _variable_name, string _variable_index, string _) = FiMMethods.MatchArray2( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, _variable_name );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, report, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToDouble(var.GetValue(i).Value)-1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else {
                                    throw FiMError.CreatePartial( FiMErrorType.VARIABLE_DOESNT_EXIST, variable_name );
                                }
                            }
                        }
                        break;

                        case TokenTypes.RETURN: {
                            object value = FiMMethods.ParseVariable((string)line.Arguments, report, CombineAllVariables(), out VariableTypes t, fallback: this.ReturnType);
                            if( t != this.ReturnType )
                                throw FiMError.CreatePartial( FiMErrorType.PARAGRAPH_RETURNED_DIFFERENT_TYPE, this.ReturnType, t );
                            return new FiMVariableStruct( value, this.ReturnType );
                        }

                        case TokenTypes.IF_STATEMENT: {
                            FiMIfStatement statement = (FiMIfStatement)line.Arguments;
                            index = statement.Conditions.LastOrDefault().Lines.End;

                            for( int i = 0; i < statement.Conditions.Count; i++ ) {
                                bool result;

                                if( i == statement.Conditions.Count-1 && statement.HasElse ) result = true;
                                else result = FiMConditional.Evaluate( statement.Conditions[i].Condition, report, CombineAllVariables() );

                                if( result == true ) {

                                    var if_lines = statement.Conditions[i].Lines;

                                    var if_result = Execute(
                                        if_lines.Start, if_lines.End,
                                        out var changedVars, CombineVariables()
                                    );
                                    
                                    foreach( var v in changedVars )
                                        SetVariable( v.Key, v.Value );

                                    if( if_result.Value != null )
                                        return if_result;

                                    break;       
                                }
                            }
                        }
                        break;
                        case TokenTypes.WHILE_STATEMENT: {
                            FiMWhileStatement statement = (FiMWhileStatement)line.Arguments;
                            index = statement.Lines.End;

                            bool result = false;
                            do {
                                result = FiMConditional.Calculate( statement.Condition, report, CombineAllVariables() );
                                if( result ) {

                                    var while_result = Execute(
                                        statement.Lines.Start, statement.Lines.End,
                                        out var changedVars, CombineVariables()
                                    );
                                    
                                    foreach( var v in changedVars ) SetVariable( v.Key, v.Value );
                                    if( while_result.Value!= null ) return while_result;
                                }
                            }
                            while( result );
                        }
                        break;
                        case TokenTypes.SWITCH_STATEMENT: {
                            FiMSwitchStatement statement = (FiMSwitchStatement)line.Arguments;
                            index = statement.EndIndex;

                            string lvariable = statement.Switch;
                            FiMVariable lvar = GetVariable(lvariable);
                            bool success = false;

                            foreach( string s in statement.Case.Keys ) {

                                string rvariable = s;
                                if( FiMConditional.Calculate(lvariable, "==", rvariable, report, CombineAllVariables()) ) {
                                    
                                    success = true;

                                    var statement_lines = statement.Case[ s ];

                                    var switch_result = Execute(
                                        statement_lines.Start, statement_lines.End,
                                        out var changedVars, CombineVariables()
                                    );

                                    foreach( var v in changedVars ) SetVariable( v.Key, v.Value );
                                    if( switch_result.Value != null ) return switch_result;

                                    break;
                                }

                            }

                            if( !success && statement.HasDefault() ) {

                                var switch_result = Execute(
                                    statement.Default.Start, statement.Default.End,
                                    out var changedVars, CombineVariables()
                                );

                                foreach( var v in changedVars ) SetVariable( v.Key, v.Value );
                                if( switch_result.Value != null ) return switch_result;

                            }
                            
                        }
                        break;

                        case TokenTypes.FOR_TO_STATEMENT: {
                            var statement = (FiMForToStatement)line.Arguments;
                            index = statement.Lines.End;

                            if( HasVariable(statement.Element.Name) ) throw FiMError.CreatePartial( FiMErrorType.VARIABLE_ALREADY_EXISTS, statement.Element.Name );
                            
                            double min = Convert.ToDouble( FiMMethods.ParseVariable(statement.Range.From, report, CombineAllVariables(), out var min_type) );
                            double max = Convert.ToDouble( FiMMethods.ParseVariable(statement.Range.To, report, CombineAllVariables(), out var max_type) );

                            if( min_type != VariableTypes.INTEGER || max_type != VariableTypes.INTEGER ) throw FiMError.CreatePartial( FiMErrorType.RANGE_MUST_BE_NUMBER );

                            double interval = 1;
                            VariableTypes interval_type = VariableTypes.INTEGER;
                            if( statement.Range.By.Length > 0 ) interval = Convert.ToDouble( FiMMethods.ParseVariable(statement.Range.By, report, CombineAllVariables(), out interval_type) );
                            else if( min > max ) interval = -1;
                            if( interval_type != VariableTypes.INTEGER ) throw FiMError.CreatePartial( FiMErrorType.INTERVAL_MUST_BE_NUMBER );

                            while( interval > 0 ? min <= max : min >= max ) {
                                var _t = CombineVariables();
                                _t.Add(
                                    statement.Element.Name,
                                    new FiMVariable(
                                        min,
                                        VariableTypes.INTEGER,
                                        true,
                                        false
                                    )
                                );

                                var for_result = Execute(
                                    statement.Lines.Start, statement.Lines.End,
                                    out var changedVars, _t
                                );
                                
                                foreach( var v in changedVars ) SetVariable( v.Key, v.Value );
                                if( for_result.Value != null ) return for_result;

                                min += interval;
                            }
                        }
                        break;
                        case TokenTypes.FOR_IN_STATEMENT: {
                            var statement = (FiMForInStatement)line.Arguments;
                            index = statement.Lines.End;

                            FiMVariable variable = GetVariable( statement.Variable );
                            if( !FiMMethods.IsVariableTypeArray( variable.Type, true ) ) 
                                throw FiMError.CreatePartial( FiMErrorType.FORIN_VARIABLE_ARRAY_ONLY );

                            if( FiMMethods.VariableTypeArraySubType( variable.Type ) != statement.Element.Type )
                                throw FiMError.CreatePartial( FiMErrorType.UNEXPECTED_TYPE, statement.Element.Type, FiMMethods.VariableTypeArraySubType( variable.Type ) );

                            FiMForInStruct Exec( FiMVariable _v ) {
                                var _t = CombineVariables();
                                _t.Add(
                                    statement.Element.Name,
                                    _v
                                );

                                var for_result = Execute(
                                    statement.Lines.Start, statement.Lines.End,
                                    out var changedVars, _t
                                );
                                
                                return new FiMForInStruct( for_result, changedVars );
                            }

                            if( variable.Type == VariableTypes.STRING ) {
                                string _v = variable.GetValue().Value.ToString();
                                foreach( char c in _v ) {
                                    var ch = Exec(new FiMVariable( c, statement.Element.Type, true, false ));

                                    foreach( var v in ch.Changed ) SetVariable( v.Key, v.Value );
                                    if( ch.Returned.Value != null ) return ch.Returned;
                                }
                            }
                            else {
                                var _v = variable.GetRawValue() as Dictionary<int,object>;
                                _v = _v.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value); // soart
                                foreach( object p in _v.Values ) {
                                    var ch = Exec(new FiMVariable( p, statement.Element.Type, true, false ));

                                    foreach( var v in ch.Changed ) SetVariable( v.Key, v.Value );
                                    if( ch.Returned.Value != null ) return ch.Returned;
                                }
                            }
                        }
                        break;

                        //--\\
                        case TokenTypes.COMMENT:
                        case TokenTypes.IGNORE: break;
                        default: {
                            // Console.WriteLine($"Haven't handled { line.Item2 } yet!");
                        }
                        break;

                    }
                    }
                    catch( FiMPartialException partial ) {
                        throw FiMError.Create( partial, line.Line, index );
                    }
                }

                index++;
            }

            return new FiMVariableStruct( null, VariableTypes.UNDEFINED );
        }
        internal FiMVariableStruct Execute(List<object> Params = null)
        {
            var LocalVariables = new Dictionary<string, FiMVariable>();

            if( Params != null ) {
                // This assumes:
                // Params is the exact count as Parameters
                // And that both types are the same
                for( int i = 0; i < Params.Count; i++ ) {
                    LocalVariables.Add( this.Parameters[i].Name, new FiMVariable(
                        Params[i],
                        this.Parameters[i].Type,
                        false, //true,
                        FiMMethods.IsVariableTypeArray( this.Parameters[i].Type )
                    ) );
                }
            }

            FiMVariableStruct str = Execute(
                Lines.Start, Lines.End,
                out var _,
                LocalVariables
            );

            if( str.Value == null && this.ReturnType != VariableTypes.UNDEFINED ) {
                throw FiMError.Create( FiMErrorType.PARAGRAPH_NO_RETURN );
            }
            if( str.Value != null && this.IsMain ) {
                throw new FiMException("Main Paragraph cannot return a variable!");
            }

            return str;
        }

        /// <summary>
        /// Executes a FiM++ paragraph.
        /// </summary>
        /// <param name="Params">The parameters to be sent to the paragraph. See <c>FiMParagaph.Parameters</c> for more info</param>
        /// <returns>Returns the appropriate variable the paragraph returhs (If not a main paragraph).</returns>
        public dynamic Execute(params object[] Params) {
            (object value, VariableTypes type) = this.Execute( Params.ToList() );

            if( value == null ) return null;
            switch( type ) {
                case VariableTypes.STRING: return Convert.ToString(value);

                case VariableTypes.BOOLEAN_ARRAY:
                    return (value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToBoolean(v.Value));
                case VariableTypes.DOUBLE_ARRAY:
                    return (value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToDouble(v.Value));
                case VariableTypes.STRING_ARRAY:
                    return (value as Dictionary<int, object>).ToDictionary(k => k.Key, v => Convert.ToString(v.Value));
                
                default: return value;
            }
        }
    }

}