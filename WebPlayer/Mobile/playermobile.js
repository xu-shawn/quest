﻿function panesVisible(visible) {
//    if (visible) {
//        $("#gamePanes").show();
//        $("#gameContent").css("width", "700px");
//        $("#txtCommand").css("width", "680px");
//        $("#updating").css("margin-left", "185px");
//        $("#gamePanel").css("width", "700px");
//    }
//    else {
//        $("#gamePanes").hide();
//        $("#gameContent").css("width", "910px");
//        $("#txtCommand").css("width", "890px");
//        $("#updating").css("margin-left", "405px");
//        $("#gamePanel").css("width", "910px");
//    }
}

function scrollToEnd() {
    var newScrollTop = beginningOfCurrentTurnScrollPosition - 15;
    if ($("#gameBorder").height() > $(window).height()) {
        $('html, body').animate({ scrollTop: newScrollTop }, 200);
    }
}

function setBackground(col) {
    $("#gameBorder").css("background-color", col);
    $("#txtCommandDiv").css("background-color", col);
    $("body").css("background-color", col);
}

function setPanelHeight() {
}

function setPanelContents(html) {
}

var resizeTimer;

function ui_init() {
    //    resizeUI();
    //    $(window).resize(function () {
    //        clearTimeout(resizeTimer);
    //        resizeTimer = setTimeout(function () {
    //            resizeUI();
    //        }, 100);
    //    });
    //    document.addEventListener("orientationChanged", resizeUI);

    SetMenuFontSize("20px");
    _allowMenuFontSizeChange = false;

    var options = [
        { title: "Inventory", action: { type: "fn", callback: "tabMenu('inventory');"} },
        { title: "Location", action: { type: "fn", callback: "tabMenu('objects');"} },
        { title: "Exits", action: { type: "fn", callback: "tabMenu('exits');"} },
        { title: "More", action: { type: "fn", callback: "tabMenu('more');"} }
    ];
    $("#tabButton").jjmenu("both", options, {}, { show: "fadeIn", speed: 100, xposition: "left", yposition: "auto", "orientation": "auto" });

    $("button.backButton span").html("&lt; Back to game");
    $("button.backButton").click(function () { tabMenu('game'); });
    $("#cmdLook").click(function () { sendCommand("look"); });
    $("#cmdRestart").click(function () { window.location.reload(); });
    $("#cmdUndo").click(function () { sendCommand("undo"); });
    $("#cmdWait").click(function () { sendCommand("wait"); });
}

function tabMenu(id) {
    $("div[id^=jjmenu]").remove();
    tabSelected(id);
}

function resizeUI() {
}

function beginWait() {
    _waitMode = true;
    $("#txtCommand").fadeOut(400, function () {
        $("#endWaitLink").fadeTo(400, 1);
    });
    markScrollPosition();
}

function sessionTimeout() {
    disableInterface();
    $("#sessionTimeoutDiv").show();
}

function gameFinished() {
    disableInterface();
    $("#gameOverDiv").show();
}

function disableInterface() {
    $("#txtCommandDiv").hide();
    $("#gamePanesRunning").hide();
}

var currentTab = "game";

function tabSelected(tab) {
    if (tab != currentTab) {
        var olddiv = divNameForTab(currentTab);
        var newdiv = divNameForTab(tab);
        currentTab = tab;
        if (tab != "game") {
            $("#gameContent").css("visibility", "hidden");
        }
        newdiv.show();
        if (tab == "game") {
            $('html, body').animate({ scrollTop: $(document).height() }, 10);
        }
        else {
            $('html, body').animate({ scrollTop: 0 }, 10);
        }
        olddiv.hide();
        $("#gameOptions").hide();
        if (tab == "game") {
            setTimeout(function () {
                $("#gameContent").css("visibility", "visible");
            }, 50);
        }
    }
}

function divNameForTab(tab) {
    switch (tab) {
        case "game":
            return $("#gameContent");
        case "inventory":
            return $("#gamePanes");
        case "objects":
            return $("#gameObjects");
        case "exits":
            return $("#gameExits");
        case "more":
            return $("#gameMore");
    }
}

function setInterfaceString(name, text) {
    switch (name) {
        case "InventoryLabel":
            $("#inventoryLabel").html(text);
            break;
        case "PlacesObjectsLabel":
            // not implemented on mobile WebPlayer
            break;
        case "CompassLabel":
            $("#compassLabel").html(text);
            break;
        case "InButtonLabel":
            $("#cmdCompassIn").attr("value", text);
            break;
        case "OutButtonLabel":
            $("#cmdCompassOut").attr("value", text);
            break;
        case "EmptyListLabel":
            break;
        case "NothingSelectedLabel":
            break;
    }
}

function updateLocation(text) {
    $("#placesObjectsLabel").html(text);
}

function afterSendCommand() {
    tabSelected("game");
}

function afterSave() {
    tabSelected("game");
}

var lastPaneLinkId = 0;

function updateList(listName, listData) {
    var listElement = "";
    var emptyListLabel = "";

    if (listName == "inventory") {
        listElement = "#inventoryList";
        emptyListLabel = "#inventoryEmpty";
    }

    if (listName == "placesobjects") {
        listElement = "#objectsList";
        emptyListLabel = "#placesObjectsEmpty";
    }

    $(listElement).empty();
    $(listElement).show();
    var listcount = 0;
    var anyItem = false;

    $.each(listData, function (key, value) {
        var splitString = value.split(":");
        var objectDisplayName = splitString[0];
        var objectVerbs = splitString[1];

        if (listName == "inventory" || $.inArray(objectDisplayName, _compassDirs) == -1) {
            listcount++;
            lastPaneLinkId++;
            var paneLinkId = "paneLink" + lastPaneLinkId;
            $(listElement).append(
                "<li id=\"" + paneLinkId + "\" href=\"#\">" + objectDisplayName + "</li>"
            );
            $("#" + paneLinkId).bind("touchstart", function () {
                $(this).addClass("elementListHover")
            });
            $("#" + paneLinkId).bind("touchend", function () {
                $(this).removeClass("elementListHover")
            });
            bindMenu(paneLinkId, objectVerbs, objectDisplayName);
            anyItem = true;
        }
    });
    $(listElement + " li:last-child").addClass('last-child')
    if (listcount == 0) $(listElement).hide();
    if (anyItem) {
        $(emptyListLabel).hide();
    }
    else {
        $(emptyListLabel).show();
    }
}