using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Text.RegularExpressions;

using FiMSharp.GlobalVars;

namespace FiMSharp
{

        public class FiMParagraph
    {
        public string Name;
        public FiMParagraph( string paragraph_name_ )
        {
            this.Name = paragraph_name_;
        }

        public (int, int) Lines = (-1, -1);
        public List<(string, VariableTypes)> Parameters;
        public VariableTypes ReturnType;
        public bool IsMain;

        public (object, VariableTypes) Execute(FiMReport report, int lineBegin, int lineEnd, out Dictionary<string,FiMVariable> changedVariables, Dictionary<string, FiMVariable> variables = null)
        {
            changedVariables = new Dictionary<string, FiMVariable>();
            Dictionary<string, FiMVariable> localVariables = new Dictionary<string, FiMVariable>();

            // Ain't sure if this is efficient lol
            FiMVariable GetVariable( string var_name ) {
                if( report.Variables.ContainsKey(var_name) ) return report.Variables[ var_name ];
                if( variables != null && variables.ContainsKey(var_name) ) return variables[ var_name ];
                return localVariables[ var_name ];
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
                    switch( line.Item2 ) {

                        case TokenTypes.RUN: {
                            string _pname = (string)line.Item3;
                            if( _pname.Contains(" using ") ) _pname = _pname.Split(new string[] {" using "}, StringSplitOptions.None)[0].Trim();

                            if( !report.Paragraphs.ContainsKey(_pname) )
                                throw new FiMException($"Paragraph {_pname} doesn't exist!");

                            FiMMethods.ParseVariable( (string)line.Item3, report, CombineAllVariables(), out VariableTypes _ );
                        }
                        break;
                        case TokenTypes.PRINT: {
                            string output = FiMMethods.SanitizeString( (string)line.Item3, report, CombineAllVariables() );
                            if( output.StartsWith("\"") && output.EndsWith("\"") )
                                output = output.Substring( 1, output.Length-2 );
                            Console.WriteLine($"[FiM] { output }");
                        }
                        break;
                        case TokenTypes.READ: {
                            string variable_name = (string)line.Item3;

                            if( !HasVariable(variable_name) )
                                throw new FiMException(
                                    FiMMethods.CreateExceptionString(
                                        $"Variable ${ variable_name } doesn't exist!",
                                        line.Item1, index+1
                                    )
                                );
                            FiMVariable var = GetVariable( variable_name );

                            Console.Write("[FiM Input]: ");
                            string input = Console.ReadLine();

                            if( var.Type == VariableTypes.STRING ) input = $"\"{input}\"";
                            else if( var.Type == VariableTypes.CHAR ) {
                                if( input.Length != 1 ) throw new FiMException("Too many characters in character literal");
                                input = $"\'{input}\'";
                            }

                            object new_value = FiMMethods.ParseVariable( input, report, CombineAllVariables(), out var input_type, fallback: VariableTypes.STRING );
                            if( input_type != var.Type ) 
                                throw new FiMException( $"Expected type { var.Type }, got { input_type }" );

                            SetVariableValue( variable_name, new_value );
                        }
                        break;
                        
                        case TokenTypes.CREATE_VARIABLE: {
                            var new_variable = FiMMethods.VariableFromTokenizer( report, CombineAllVariables(), line.Item3 );
                            SetVariable( new_variable.Item1, new_variable.Item2 );
                        }
                        break;
                        case TokenTypes.VARIABLE_REPLACE: {
                            List<object> args = line.Item3 as List<object>;
                            string variable_name = (string)args[0];
                            string _variable_value = (string)args[1];

                            if( !HasVariable(variable_name) )
                                throw new FiMException( $"Variable ${ variable_name } doesn't exist!" );

                            FiMVariable var = GetVariable( variable_name );
                            VariableTypes _expected_type = args.Count == 3 ? (VariableTypes)args[2] : var.Type;

                            object new_value = FiMMethods.ParseVariable( _variable_value, report, CombineAllVariables(), out VariableTypes _got_type, run_once: false, fallback: _expected_type );
                            if( _got_type != _expected_type ) {
                                if( _expected_type == VariableTypes.STRING ) {
                                    new_value = new_value.ToString(); // :^)
                                } else {
                                    throw new FiMException( $"Expected type { _expected_type }, got { _got_type }" );
                                }
                            }
                                
                            SetVariableValue( variable_name, new_value );
                            if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                        }
                        break;

                        case TokenTypes.ARRAY_MODIFY: {
                            List<object> args = line.Item3 as List<object>;
                            string variable_name = (string)args[0];
                            int array_index = (int)args[1];
                            string keyword = (string)args[2];
                            VariableTypes expected_type;
                            string value;

                            (int, FiMVariable) result = FiMMethods.ParseArray( array_index, variable_name, CombineAllVariables() );

                            if( args.Count == 5 ) {
                                expected_type = (VariableTypes)args[3];
                                value = (string)args[4];
                            } else {
                                expected_type = FiMMethods.VariableTypeArraySubType( result.Item2.Type );
                                value = (string)args[3];
                            }

                            object new_value = FiMMethods.ParseVariable( value, report, CombineAllVariables(), out VariableTypes _got_type, fallback: expected_type );
                            if( _got_type != expected_type ) {
                                throw new Exception(
                                    $"Expected type { expected_type }, got { _got_type }"
                                );
                            }

                            result.Item2.SetArrayValue( result.Item1, new_value );
                            if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                        }
                        break;
                        case TokenTypes.ARRAY_MODIFY2: {
                            List<object> args = line.Item3 as List<object>;
                            string variable_name = (string)args[0];
                            string variable_index = (string)args[1];
                            string keyword = (string)args[2];
                            string value = (string)args[3];

                            (int, FiMVariable) result = FiMMethods.ParseArray( variable_index, variable_name, report, CombineAllVariables() );

                            VariableTypes expected_type = FiMMethods.VariableTypeArraySubType( result.Item2.Type );
                            object new_value = FiMMethods.ParseVariable( value, report, CombineAllVariables(), out VariableTypes _got_type, fallback: expected_type );
                            if( _got_type != expected_type ) {
                                throw new Exception(
                                    $"Expected type { expected_type }, got { _got_type }"
                                );
                            }

                            result.Item2.SetArrayValue( result.Item1, new_value );
                            if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                        }
                        break;

                        case TokenTypes.VARIABLE_INCREMENT: {
                            string variable_name = (string)line.Item3;
                            if( HasVariable(variable_name) ) {
                                FiMVariable variable = GetVariable( variable_name );
                                if( variable.Type != VariableTypes.INTEGER )
                                    throw new FiMException( $"Can only increment numbers!");

                                SetVariableValue( variable_name, Convert.ToSingle(variable.GetValue().Item1) + 1 );
                                if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                            } else {
                                if( FiMMethods.IsMatchArray1(variable_name, true) ) {
                                    (string _result, string _variable_name, int _variable_index, string _) = FiMMethods.MatchArray1( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw new FiMException( $"Variable ${ _variable_name } doesn't exist!" );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToSingle(var.GetValue(i).Item1)+1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else if( FiMMethods.IsMatchArray2(variable_name, true) ) {
                                    (string _result, string _variable_name, string _variable_index, string _) = FiMMethods.MatchArray2( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw new FiMException( $"Variable ${ _variable_name } doesn't exist!" );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, report, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToSingle(var.GetValue(i).Item1)+1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else {
                                    throw new FiMException( $"Variable ${ variable_name } doesn't exist!" );
                                }
                            }
                        }
                        break;
                        case TokenTypes.VARIABLE_DECREMENT: {
                            string variable_name = (string)line.Item3;
                            if( HasVariable(variable_name) ) {
                                FiMVariable variable = GetVariable( variable_name );
                                if( variable.Type != VariableTypes.INTEGER )
                                    throw new FiMException( $"Can only increment numbers!");

                                SetVariableValue( variable_name, Convert.ToSingle(variable.GetValue().Item1) - 1 );
                                if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                            } else {
                                if( FiMMethods.IsMatchArray1(variable_name, true) ) {
                                    (string _result, string _variable_name, int _variable_index, string _) = FiMMethods.MatchArray1( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw new FiMException( $"Variable ${ _variable_name } doesn't exist!" );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToSingle(var.GetValue(i).Item1)-1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else if( FiMMethods.IsMatchArray2(variable_name, true) ) {
                                    (string _result, string _variable_name, string _variable_index, string _) = FiMMethods.MatchArray2( variable_name ,true );
                                    if( !HasVariable(_variable_name) )
                                        throw new FiMException( $"Variable ${ _variable_name } doesn't exist!" );
                                    (int i, FiMVariable var) = FiMMethods.ParseArray( _variable_index, _variable_name, report, CombineAllVariables() );
                                    var.SetArrayValue( i, Convert.ToSingle(var.GetValue(i).Item1)-1 );

                                    if( IsScopeVariable( variable_name ) ) changedVariables[variable_name] = GetVariable(variable_name);
                                }
                                else {
                                    throw new FiMException( $"Variable ${ variable_name } doesn't exist!" );
                                }
                            }
                        }
                        break;

                        case TokenTypes.RETURN: {
                            object value = FiMMethods.ParseVariable((string)line.Item3, report, CombineAllVariables(), out VariableTypes t, fallback: this.ReturnType);
                            if( t != this.ReturnType )
                                throw new FiMException( $"Returned a variable of different type" );
                            return (value, this.ReturnType );
                        }

                        case TokenTypes.IF_STATEMENT: {
                            FiMIfStatement statement = (FiMIfStatement)line.Item3;
                            index = statement.Conditions.LastOrDefault().Item2.Item2;

                            for( int i = 0; i < statement.Conditions.Count; i++ ) {
                                bool result;

                                if( i == statement.Conditions.Count-1 && statement.HasElse ) result = true;
                                else result = FiMConditional.Evaluate( statement.Conditions[i].Item1, report, CombineAllVariables() );

                                if( result == true ) {

                                    var if_lines = statement.Conditions[i].Item2;

                                    var if_result = Execute(
                                        report,
                                        if_lines.Item1, if_lines.Item2,
                                        out var changedVars, CombineVariables()
                                    );
                                    
                                    foreach( var v in changedVars )
                                        SetVariable( v.Key, v.Value );

                                    if( if_result.Item1 != null )
                                        return if_result;

                                    break;       
                                }
                            }
                        }
                        break;
                        case TokenTypes.WHILE_STATEMENT: {
                            FiMWhileStatement statement = (FiMWhileStatement)line.Item3;
                            index = statement.Lines.Item2;

                            bool result = false;
                            do {
                                result = FiMConditional.Calculate( statement.Condition, report, CombineAllVariables() );
                                if( result ) {

                                    var while_result = Execute(
                                        report,
                                        statement.Lines.Item1, statement.Lines.Item2,
                                        out var changedVars, CombineVariables()
                                    );
                                    
                                    foreach( var v in changedVars ) SetVariable( v.Key, v.Value );
                                    if( while_result.Item1 != null ) return while_result;

                                }
                            }
                            while( result );
                        }
                        break;
                        case TokenTypes.SWITCH_STATEMENT: {
                            FiMSwitchStatement statement = (FiMSwitchStatement)line.Item3;
                            index = statement.EndIndex;

                            string lvariable = statement.Switch;
                            FiMVariable lvar = GetVariable(lvariable);
                            bool success = false;

                            foreach( string s in statement.Case.Keys ) {

                                string rvariable = s;
                                if( FiMConditional.Calculate(lvariable, "==", rvariable, report, CombineAllVariables()) ) {
                                    
                                    success = true;

                                    (int, int) statement_lines = statement.Case[ s ];

                                    var switch_result = Execute(
                                        report,
                                        statement_lines.Item1, statement_lines.Item2,
                                        out var changedVars, CombineVariables()
                                    );

                                    foreach( var v in changedVars )
                                        SetVariable( v.Key, v.Value );

                                    if( switch_result.Item1 != null )
                                        return switch_result;

                                    break;
                                }

                            }

                            if( !success && statement.HasDefault() ) {

                                var switch_result = Execute(
                                    report,
                                    statement.Default.Item1, statement.Default.Item2,
                                    out var changedVars, CombineVariables()
                                );

                                foreach( var v in changedVars )
                                    SetVariable( v.Key, v.Value );

                                if( switch_result.Item1 != null )
                                    return switch_result;

                            }
                            
                        }
                        break;

                        case TokenTypes.FOR_TO_STATEMENT: {
                            var statement = (FiMForToStatement)line.Item3;
                            index = statement.Lines.Item2;

                            if( HasVariable(statement.Element.Item1) )
                                throw new FiMException( $"Variable ${ statement.Element.Item1 } already exists!" );
                            
                            float min = Convert.ToSingle( FiMMethods.ParseVariable(statement.Range.Item1, report, CombineAllVariables(), out var min_type) );
                            float max = Convert.ToSingle( FiMMethods.ParseVariable(statement.Range.Item2, report, CombineAllVariables(), out var max_type) );

                            if( min_type != VariableTypes.INTEGER || max_type != VariableTypes.INTEGER )
                                throw new FiMException("Range must be a number");

                            if( max < min ) throw new FiMException("Interval is empty");

                            while( min <= max ) {

                                var _t = CombineVariables();
                                _t.Add(
                                    statement.Element.Item1,
                                    new FiMVariable(
                                        min,
                                        VariableTypes.INTEGER,
                                        true,
                                        false
                                    )
                                );

                                var for_result = Execute(
                                    report,
                                    statement.Lines.Item1, statement.Lines.Item2,
                                    out var changedVars, _t
                                );
                                
                                foreach( var v in changedVars ) SetVariable( v.Key, v.Value );
                                if( for_result.Item1 != null ) return for_result;

                                min++;

                            }
                        }
                        break;
                        case TokenTypes.FOR_IN_STATEMENT: {
                            var statement = (FiMForInStatement)line.Item3;
                            index = statement.Lines.Item2;

                            FiMVariable variable = GetVariable( statement.Variable );
                            if( !FiMMethods.IsVariableTypeArray( variable.Type, true ) ) 
                                throw new FiMException("For-in statement can only be used by arrays");

                            if( FiMMethods.VariableTypeArraySubType( variable.Type ) != statement.Element.Item2 )
                                throw new FiMException("Mismatch types");

                            ((object, VariableTypes), Dictionary<string, FiMVariable>) Exec( FiMVariable _v ) {
                                var _t = CombineVariables();
                                _t.Add(
                                    statement.Element.Item1,
                                    _v
                                );

                                var for_result = Execute(
                                    report,
                                    statement.Lines.Item1, statement.Lines.Item2,
                                    out var changedVars, _t
                                );
                                
                                return (for_result, changedVars);
                            }

                            if( variable.Type == VariableTypes.STRING ) {
                                string _v = variable.GetValue().Item1.ToString();
                                if( _v.StartsWith("\"") && _v.EndsWith("\"") )
                                    _v = _v.Substring(1, _v.Length-2);
                                foreach( char c in _v ) {
                                    var ch = Exec(new FiMVariable( c, statement.Element.Item2, true, false ));

                                    foreach( var v in ch.Item2 ) SetVariable( v.Key, v.Value );
                                    if( ch.Item1.Item1 != null ) return ch.Item1;
                                }
                            }
                            else {
                                var _v = variable.GetRawValue() as Dictionary<int,object>;
                                _v = _v.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value); // soart
                                foreach( object p in _v.Values ) {
                                    var ch = Exec(new FiMVariable( p, statement.Element.Item2, true, false ));

                                    foreach( var v in ch.Item2 ) SetVariable( v.Key, v.Value );
                                    if( ch.Item1.Item1 != null ) return ch.Item1;
                                }
                            }
                        }
                        break;

                        //--\\
                        case TokenTypes.COMMENT:
                        case TokenTypes.IGNORE: {
                            // do nothing
                        }
                        break;
                        default: {
                            Console.WriteLine($"Haven't handled { line.Item2 } yet!");
                        }
                        break;

                    }
                    } catch( FiMException ex ) {
                        throw new FiMException( FiMMethods.CreateExceptionString(ex.Message, line.Item1, index+1) );
                    }
                }

                index++;
            }

            return (null, VariableTypes.UNDEFINED);
        }
        public (object, VariableTypes) Execute(FiMReport report, List<object> Params = null)
        {
            var LocalVariables = new Dictionary<string, FiMVariable>();

            if( Params != null ) {
                // This assumes:
                // Params is the exact count as Parameters
                // And that both types are the same
                for( int i = 0; i < Params.Count; i++ ) {
                    LocalVariables.Add( this.Parameters[i].Item1, new FiMVariable(
                        Params[i],
                        this.Parameters[i].Item2,
                        false, //true,
                        FiMMethods.IsVariableTypeArray( this.Parameters[i].Item2 )
                    ) );
                }
            }

            (object returnValue, VariableTypes returnType) = Execute(
                report,
                Lines.Item1, Lines.Item2,
                out var _,
                LocalVariables
            );

            if( returnValue == null && this.ReturnType != VariableTypes.UNDEFINED ) {
                throw new FiMException("Paragraph didn't return a variable!");
            }

            return (returnValue, returnType);
        }
    }

}