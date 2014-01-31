function Download() {
    var RtmApiKey = "09b03090fc9303804aedd945872fdefc";
    var RtmSharedKey = "d2ffaf49356b07f9";

    getTimezones();

    function getTimezones() {
        var httpRequest = require('request');

        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.timezones.getList&api_key=" + RtmApiKey + "&format=json";

        var params = RtmSharedKey + "api_key" + RtmApiKey + "format" + "json" + "method" + "rtm.timezones.getList";

        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');

        url = url + "&api_sig=" + signature;

        httpRequest.get({
            url: url
        },
        function (err, response, body) {
            if (err) {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200) {
                console.error("Error communicating with RTM:", response);
            }
            else {
                // console.log("Request succeeded:", body);

                var data = JSON.parse(body);
                var timezones = data.rsp.timezones.timezone;

                var registrationsTable = tables.getTable('Registrations');
                registrationsTable.read({
                    success: function (registrations) {
                        var currentRegistrations = partitionGroupsByMinute(registrations);
                        currentRegistrations.forEach(function (registration) {
                            getUserSettings(registration, timezones);
                        });
                    }
                });
            }
        });
    }

    function partitionGroupsByMinute(registrations) {
        var size = 60;
        var partitions = [];

        for (var i = 0; i < 60; i++) {
            partitions[i] = [];
        }

        var index = 0;
        registrations.forEach(function (registration) {
            partitions[index].push(registration);
            index++;

            if (index == 60) index = 0;
        });

        var now = new Date();
        var minute = now.getMinutes();

        return partitions[minute];
    }

    function getUserSettings(registration, timezones) {
        var httpRequest = require('request');

        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.settings.getList&api_key=" + RtmApiKey + "&format=json&auth_token=" + registration.authenticationToken;

        var params = RtmSharedKey + "api_key" + RtmApiKey + "auth_token" + registration.authenticationToken + "format" + "json" + "method" + "rtm.settings.getList";

        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');

        url = url + "&api_sig=" + signature;

        httpRequest.get({
            url: url
        },
        function (err, response, body) {
            if (err) {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200) {
                console.error("Error communicating with RTM:", response);
            }
            else {
                // console.log("Request succeeded:", body);

                var data = JSON.parse(body);
                var timezone = data.rsp.settings.timezone;

                timezones.forEach(function (item) {
                    if (item.name == timezone) {
                        getTasks(registration, item.current_offset);
                    }
                });
            }
        });
    }    

    function getTasks(registration, timezoneOffset) {
        var httpRequest = require('request');

        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.tasks.getList&api_key=" + RtmApiKey + "&format=json&auth_token=" + registration.authenticationToken + "&filter=status:incomplete AND due:today";

        var params = RtmSharedKey + "api_key" + RtmApiKey + "auth_token" + registration.authenticationToken + "filter" + "status:incomplete AND due:today" + "format" + "json" + "method" + "rtm.tasks.getList";

        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');

        url = url + "&api_sig=" + signature;

        httpRequest.get({
            url: url
        },
        function (err, response, body) {
            if (err) {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200) {
                console.error("Error communicating with RTM:", response);
            }
            else {
                // console.log("Request succeeded:", body);

                var data = JSON.parse(body);
                var lists = data.rsp.tasks.list;

                if (lists !== undefined) {
                    lists.forEach(function (item1) {
                        var tasks = item1.taskseries;

                        var reminderInterval = registration.reminderInterval * 60000;
                        reminderInterval = reminderInterval;

                        try {
                            tasks.forEach(function (item2) {
                                if (item2.task.has_due_time == 1) {
                                    var now = new Date();
                                    var dueDateTime = new Date(item2.task.due);
                                    var localDueDateTime = new Date(dueDateTime.getTime() + timezoneOffset * 1000);

                                    var start = new Date(now.getTime() + reminderInterval - 30000);

                                    if (start < dueDateTime) {
                                        var dueTime = formatDateTime(localDueDateTime);
                                        insertReminder(registration, item2, item2.name, "This task is due at " + dueTime + " today.", dueDateTime);
                                    }
                                }
                            });
                        }
                        catch (ex) {
                            if (tasks.task.has_due_time == 1) {
                                var now = new Date();
                                var dueDateTime = new Date(tasks.task.due);
                                var localDueDateTime = new Date(dueDateTime.getTime() + timezoneOffset * 1000);

                                var start = new Date(now.getTime() + reminderInterval - 30000);

                                if (start < dueDateTime) {
                                    var dueTime = formatDateTime(localDueDateTime);
                                    insertReminder(registration, tasks, tasks.name, "This task is due at " + dueTime + " today.", dueDateTime);
                                }
                            }
                        }
                    });
                }
            }
        });
    }

    function formatDateTime(dateTime) {
        var hour = dateTime.getHours();
        var minutes = dateTime.getMinutes();
        var ampm = "";

        if (hour < 12) {
            ampm = "am";
        }
        else {
            ampm = "pm";
        }

        if (hour == 0) {
            hour = 12;
        }

        if (hour > 12) {
            hour = hour - 12;
        }

        minutes = minutes + "";

        if (minutes.length == 1) {
            minutes = "0" + minutes;
        }

        return hour + ":" + minutes + ampm;
    }

    function insertReminder(registration, data, text1, text2, dueDateTime) {
        var remindersTable = tables.getTable('Reminders');
        remindersTable.insert({
            id: data.id,
            text1: text1,
            text2: text2,
            dueDateTime: dueDateTime,
            registrationId: registration.id
        },
        {
            success: function (reminders) {
                // console.log("Record inserted.", reminders);
            },
            error: function (reminders) {
                updateReminder(registration, data, text1, text2, dueDateTime);
            }
        });
    }

    function updateReminder(registration, data, text1, text2, dueDateTime) {
        var remindersTable = tables.getTable('Reminders');
        remindersTable.update({
            id: data.id,
            text1: text1,
            text2: text2,
            dueDateTime: dueDateTime,
            registrationId: registration.id
        },
        {
            success: function (reminders) {
                // console.log("Record updated.", reminders);
            },
            error: function (reminders) {
                console.error('Could not insert or update record.', reminders);
            }
        });
    }
}
