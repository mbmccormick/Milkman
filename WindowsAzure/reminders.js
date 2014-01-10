function Reminders()
{
    var RtmApiKey = "09b03090fc9303804aedd945872fdefc";
    var RtmSharedKey = "d2ffaf49356b07f9";

    var registrationsTable = tables.getTable('Registrations');
    registrationsTable.read({
        success: function(registrations)
        {
            registrations.forEach(function(registration)
            {
                processRegistration(registration);
            });
        }
    });
    
    function processRegistration(registration)
    {
        getTimezone(registration);
    }

    function getTimezone(registration)
    {
        var httpRequest = require('request');
                    
        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.settings.getList&api_key=" + RtmApiKey + "&format=json&auth_token=" + registration.authenticationToken; 
        
        var params = RtmSharedKey + "api_key" + RtmApiKey + "auth_token" + registration.authenticationToken + "format" + "json" + "method" + "rtm.settings.getList";
    
        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');
    
        url = url + "&api_sig=" + signature;
        
        httpRequest.get({
            url: url
        },
        function(err, response, body)
        {
            if (err)
            {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200)
            {
                console.error("Error communicating with RTM:", response);
            }
            else
            {
                // console.log("Request succeeded:", body);
                
                var data = JSON.parse(body);
                var timezone = data.rsp.settings.timezone;
    
                getTimezoneOffset(registration, timezone);
            }
        });
    }
    
    function getTimezoneOffset(registration, timezone)
    {
        var httpRequest = require('request');
                    
        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.timezones.getList&api_key=" + RtmApiKey + "&format=json"; 
        
        var params = RtmSharedKey + "api_key" + RtmApiKey + "format" + "json" + "method" + "rtm.timezones.getList";
    
        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');
    
        url = url + "&api_sig=" + signature;
        
        httpRequest.get({
            url: url
        },
        function(err, response, body)
        {
            if (err)
            {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200)
            {
                console.error("Error communicating with RTM:", response);
            }
            else
            {
                // console.log("Request succeeded:", body);
                
                var data = JSON.parse(body);
                var timezones = data.rsp.timezones.timezone;
    
                timezones.forEach(function (item)
                {
                    if (item.name == timezone)
                    {
                        getTasks(registration, item.current_offset);
                    }
                });
            }
        });
    }
    
    function getTasks(registration, timezoneOffset)
    {
        var httpRequest = require('request');
                    
        var url = "https://api.rememberthemilk.com/services/rest/?method=rtm.tasks.getList&api_key=" + RtmApiKey + "&format=json&auth_token=" + registration.authenticationToken + "&filter=status:incomplete AND due:today"; 
        
        var params = RtmSharedKey + "api_key" + RtmApiKey + "auth_token" + registration.authenticationToken + "filter" + "status:incomplete AND due:today" + "format" + "json" + "method" + "rtm.tasks.getList";
    
        var crypto = require('crypto');
        var signature = crypto.createHash('md5').update(params).digest('hex');
    
        url = url + "&api_sig=" + signature;
        
        httpRequest.get({
            url: url
        },
        function(err, response, body)
        {
            if (err)
            {
                console.error("Unable to connect to RTM:", response);
            }
            else if (response.statusCode != 200)
            {
                console.error("Error communicating with RTM:", response);
            }
            else
            {
                // console.log("Request succeeded:", body);
                
                var data = JSON.parse(body);
                var lists = data.rsp.tasks.list;
    
                if (lists !== undefined)
                {
                    lists.forEach(function (item1)
                    {
                        var tasks = item1.taskseries;
                        
                        var reminderInterval = registration.reminderInterval * 60000;
                        reminderInterval = reminderInterval;
                        
                        try
                        {
                            tasks.forEach(function (item2)
                            {
                                if (item2.task.has_due_time == 1)
                                {
                                    var now = new Date();
                                    var dueDateTime = new Date(item2.task.due);
                                    var localDueDateTime = new Date(dueDateTime.getTime() + timezoneOffset * 1000);
                                    
                                    var start = new Date(now.getTime() + reminderInterval - 60000);
                                    var end = new Date(now.getTime() + reminderInterval);
                                    
                                    if (dueDateTime > start &&
                                        dueDateTime <= end)
                                    {
                                        var dueTime = formatDateTime(localDueDateTime);
                                        sendToastNotification(registration, item2.name, "This task is due at " + dueTime + " today.");
                                    }
                                }
                            });
                        }
                        catch (ex)
                        {
                            if (tasks.task.has_due_time == 1)
                            {
                                var now = new Date();
                                var dueDateTime = new Date(tasks.task.due);
                                var localDueDateTime = new Date(dueDateTime.getTime() + timezoneOffset * 1000);
                                
                                var start = new Date(now.getTime() + reminderInterval - 60000);
                                var end = new Date(now.getTime() + reminderInterval);
                                
                                if (dueDateTime > start &&
                                    dueDateTime <= end)
                                {
                                    var dueTime = formatDateTime(localDueDateTime);
                                    sendToastNotification(registration, tasks.name, "This task is due at " + dueTime + " today.");
                                }
                            }
                        }
                    });
                }
            }
        });
    }
    
    function formatDateTime(dateTime)
    {
        var hour = dateTime.getHours();
        var minutes = dateTime.getMinutes();
        var ampm = "";
        
        if (hour < 12)
        {
            ampm = "am";
        }
        else
        {
            ampm = "pm";
        }
        
        if (hour == 0)
        {
            hour = 12;
        }
        
        if (hour > 12)
        {
            hour = hour - 12;
        }
        
        minutes = minutes + "";

        if (minutes.length == 1)
        {
            minutes = "0" + minutes;
        }
        
        return hour + ":" + minutes + ampm;
    }
    
    function sendToastNotification(registration, text1, text2)
    {
        push.mpns.sendToast(registration.handle, {
            text1: text1,
            text2: text2
        },
        {
            success: function(pushResponse)
            {
                console.log("Sent push:", pushResponse);
            }
        });
    }
}