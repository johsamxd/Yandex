using Yandex.Domain.Enums;

namespace Yandex.Domain.Abstractions;

public record BookingQuery(
    BookingStatus? Status = null
);