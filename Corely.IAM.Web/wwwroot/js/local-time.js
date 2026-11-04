export function format(iso, seconds, dateOnly) {
    const date = new Date(iso);
    if (Number.isNaN(date.getTime())) {
        return null;
    }

    const options = dateOnly
        ? { year: "numeric", month: "short", day: "numeric" }
        : {
            year: "numeric",
            month: "short",
            day: "numeric",
            hour: "numeric",
            minute: "2-digit",
            timeZoneName: "short",
        };
    if (seconds && !dateOnly) {
        options.second = "2-digit";
    }

    return new Intl.DateTimeFormat(undefined, options).format(date);
}
