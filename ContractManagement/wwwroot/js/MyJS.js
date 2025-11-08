$(document).ready(function () {
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/notificationHub")
        .configureLogging(signalR.LogLevel.Information)
        .build();

    connection.start().then(function () {
        console.log("SignalR Connected.");
        // عضویت در گروه کاربر جاری
        const userId = '@User.FindFirstValue(ClaimTypes.NameIdentifier)';
        connection.invoke("JoinGroup", userId);
    }).catch(function (err) {
        return console.error(err.toString());
    });

    connection.on("ReceiveNotification", function (notification) {
        console.log("Notification received:", notification);

        // نمایش نوتیفیکیشن
        $("#notificationMessage").text(notification.message);
        $("#notificationLink").attr("href", `/Contracts/Details/${notification.contractId}`);

        // نمایش toast
        const toast = new bootstrap.Toast(document.getElementById('notificationToast'));
        toast.show();

        // همچنین می‌توانید شمارنده نوتیفیکیشن‌ها را به روزرسانی کنید
        updateNotificationBadge();
    });

    function updateNotificationBadge() {
        // این تابع می‌تواند برای به روزرسانی شمارنده نوتیفیکیشن‌ها استفاده شود
        $.get("/Notifications/GetUnreadCount", function (count) {
            $("#notificationBadge").text(count).toggle(count > 0);
        });
    }
});