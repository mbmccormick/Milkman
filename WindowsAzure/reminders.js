function Reminders() {
    var RtmApiKey = "09b03090fc9303804aedd945872fdefc";
    var RtmSharedKey = "d2ffaf49356b07f9";

    var remindersTable = tables.getTable('Reminders');
    remindersTable.read({
        success: function (reminders) {
            reminders.forEach(function (reminder) {
                processReminder(reminder);
            });
        }
    });

    function processReminder(reminder) {
        var registrationsTable = tables.getTable('Registrations');
        registrationsTable.where({
            id: reminder.registrationId
        }).read({
            success: function (registrations) {
                registrations.forEach(function (registration) {
                    var now = new Date();

                    var reminderInterval = registration.reminderInterval * 60000;
                    var start = new Date(now.getTime() + reminderInterval - 60000);

                    if (start >= reminder.dueDateTime) {
                        sendToastNotification(registration, reminder.text1, reminder.text2);
                        deleteReminder(reminder);
                    }
                });
            }
        });
    }
    
    function deleteReminder(reminder) {
        var remindersTable = tables.getTable('Reminders');
        remindersTable.del(reminder.id, {
            success: function (reminders) {
                // do nothing
            }
        });
    }

    function sendToastNotification(registration, text1, text2) {
        push.mpns.sendToast(registration.handle, {
            text1: text1,
            text2: text2
        },
        {
            success: function (pushResponse) {
                console.log("Sent push:", pushResponse);
            }
        });
    }
}