namespace JC.Core.Models.Auditing;

public class LogModel : BaseCreateModel
{
    //No extra properties - logs are create or hard delete only
    //Class exists for distinction in auditing during save changes
}