using System.Collections.Generic;
using System.Linq;

using FiMSharp.GlobalVars;

namespace FiMSharp.Core
{

    public interface IFiMStatement {}

    public class FiMWhileStatement: IFiMStatement {
        public (int, int) Lines = (-1, -1);
        public string Condition;
    }

    public class FiMForStatement: IFiMStatement {
        public (int, int) Lines = (-1, -1);
        public (string, VariableTypes) Element;
    }
    public class FiMForToStatement: FiMForStatement {
        // Reminder: This can be a variable, so we're using strings instead of getting the value directly.
        public (string, string) Range;
    }
    public class FiMForInStatement: FiMForStatement {
        public string Variable;
    }

    public class FiMIfStatement: IFiMStatement {
        public List<(string, (int, int))> Conditions = new List<(string, (int, int))>();
        public bool HasElse; // If true, Conditions.Last will be the else statement
    }

    public class FiMSwitchStatement: IFiMStatement {
        public Dictionary<string, (int, int)> Case = new Dictionary<string, (int, int)>();
        public (int, int) Default = (-1, -1);
        public string Switch;
        public int EndIndex;

        public bool HasDefault() => this.Default != (-1, -1);
    }

    //

    public enum FiMStatementTypes {
        UNDEFINED = 0,
        If,
        ElseIf,
        Else,
        While,
        For,
        Switch,
    }

    public static partial class Extension {
        public static bool IsStatementStart( string line, out string keyword, out FiMStatementTypes type ) {
            keyword = "";
            type = FiMStatementTypes.UNDEFINED;

            if( Globals.Methods.If_Statement.Any(x => line.StartsWith(x)) ) {
                keyword = Globals.Methods.If_Statement.Where(x => line.StartsWith(x)).FirstOrDefault();
                type = FiMStatementTypes.If;
                return true;
            }
            if( Globals.Methods.Else_Statement.Any(x => Globals.Methods.If_Statement.Any(y => line.StartsWith(x + " " + y.ToLower()) )) ) {
                string keyword_ = Globals.Methods.Else_Statement.Where(x => line.StartsWith(x)).FirstOrDefault();
                keyword_ += " " + Globals.Methods.If_Statement.Where(x => line.StartsWith(keyword_ + " " + x.ToLower())).FirstOrDefault().ToLower();

                keyword = keyword_;
                type = FiMStatementTypes.ElseIf;
                return true;
            }
            if( Globals.Methods.Else_Statement.Any(x => line.StartsWith(x)) ) {
                keyword = Globals.Methods.Else_Statement.Where(x => line.StartsWith(x)).FirstOrDefault();
                type = FiMStatementTypes.Else;
                return true;
            }
            if( Globals.Methods.While_Statement.Any(x => line.StartsWith(x)) ) {
                keyword = Globals.Methods.While_Statement.Where(x => line.StartsWith(x)).FirstOrDefault();
                type = FiMStatementTypes.While;
                return true;
            }
            if( line.StartsWith(Globals.Methods.For_Statement) ) {
                keyword = Globals.Methods.For_Statement;
                type = FiMStatementTypes.For;
                return true;
            }
            if( line.StartsWith(Globals.Methods.Switch_Statement) ) {
                keyword = Globals.Methods.Switch_Statement;
                type = FiMStatementTypes.Switch;
                return true;
            }

            return false;
        }
        public static bool IsStatementStart( string line, FiMStatementTypes type ) {
            return IsStatementStart(line, out var _, out FiMStatementTypes t) && t == type;
        }
        public static bool IsStatementEnd( string line, FiMStatementTypes type ) {
            if(
                line.Equals( Globals.Methods.If_Statement_End ) &&
                (type == FiMStatementTypes.If || type == FiMStatementTypes.ElseIf || type == FiMStatementTypes.Else)
            ) return true;
            if( line.Equals( Globals.Methods.While_Statement_End ) && type == FiMStatementTypes.While ) return true;
            if( line.Equals( Globals.Methods.Switch_Statement_End ) && type == FiMStatementTypes.Switch ) return true;
            if( line.Equals( Globals.Methods.For_Statement_End ) && type == FiMStatementTypes.For ) return true;
            return false;
        }
    }

}