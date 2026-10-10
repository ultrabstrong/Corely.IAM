namespace Corely.IAM.Web.Extensions;

internal static class DateTimeExtensions
{
    extension(DateTime date)
    {
        public DateTime StartOfDayUtc() => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);

        public DateTime EndOfDayUtc() => date.StartOfDayUtc().AddDays(1).AddTicks(-1);
    }
}
