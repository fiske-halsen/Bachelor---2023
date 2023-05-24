const tab = {
    thresholds: 1,
    channels: 2,
    suppression: 4,
    hardwareSms: 5,
    acknowledgeText: 6
};

const alarmType = {
    system: 1,
    rising: 2,
    falling: 3,
    battery: 4,
    elapsed: 5,
    rateOfChange: 6
}

var selectedTab = tab.thresholds;
var selectedTemplate = 0;
var thresholdEditRow = -1;
var isEdit = false;
var isCreate = false;
var isCopy = false;
var loading = false;

function initialised(s) {
    if (!selectedTemplate) {

        let items = s.component.option('items');

        if (items.length > 0) {

            s.component.option('selectedItems', [items[0]]);

            let params = new URLSearchParams(window.location.search);
            let template = params.get("template");

            if (template) {
                for (let i = 0; i < items.length; i++) {
                    if (items[i].Id === parseInt(template)) {
                        s.component.option('selectedItems', [items[i]]);
                        break;
                    }
                }
            }

            let tabs = $('#templateTabs').dxTabs('instance');
            if (tabs) {
                tabs.option('disabled', false);
            }
        } else {
            noTemplates();
        }
    }
}

function noTemplates() {
    let title = document.getElementById('templateName');
    let description = document.getElementById('templateDescription');
    title.innerHTML = title.dataset.noItems;
    description.innerHTML = description.dataset.noItems;
    document.getElementById('tabularContent').innerHTML = '';

    if (document.getElementById('editTemplateButton')) $('#editTemplateButton').dxButton('instance').option('disabled', true);
    if (document.getElementById('deleteTemplateButton')) $('#deleteTemplateButton').dxButton('instance').option('disabled', true);
    if (document.getElementById('copyTemplateButton')) $('#copyTemplateButton').dxButton('instance').option('disabled', true);
}

function tabsInitialised(e) {
    e.component.option('disabled', !getSelectedTemplateId());
}

function getSelectedTemplateId() {
    let component = $('#alarmTemplateList').dxList("instance");
    let items = component.option('selectedItems');
    return items.length > 0 ? items[0].Id : null;
}

function selectTemplate(list, items) {
    if (selectedTemplate && items.length > 0) {
        for (let i = 0; i < items.length; i++) {
            if (items[i].Id === selectedTemplate) {
                if (isEdit) list.unselectAll();
                list.selectItem(items[i]);
                break;
            }
        }
    } else {
        noTemplates();
    }
}

function templateSelected(e) {
    if (e.addedItems.length > 0 && !loading) {

        if (document.getElementById('editTemplateButton')) $('#editTemplateButton').dxButton('instance').option('disabled', false);
        if (document.getElementById('deleteTemplateButton')) $('#deleteTemplateButton').dxButton('instance').option('disabled', false);
        if (document.getElementById('copyTemplateButton')) $('#copyTemplateButton').dxButton('instance').option('disabled', false);

        $('#templateName').text(e.addedItems[0].Name);
        $('#templateDescription').text(e.addedItems[0].Description);

        if (!isEdit) {
            let tabs = $('#templateTabs').dxTabs('instance');
            selectedTemplate = e.addedItems[0].Id;
            tabs.repaint();
            loadTab();
        }
    }
}

function tabSelected(e) {
    selectedTab = e.itemData.id;
    loadTab();
}

function loadTab() {
    switch (selectedTab) {
        case tab.thresholds: loadTabPartial('AlarmTemplate/ThresholdView/' + selectedTemplate); break;
        case tab.channels: loadTabPartial('AlarmTemplate/ChannelsView/' + selectedTemplate); break;
        case tab.hardwareSms: loadTabPartial('AlarmTemplate/HardwareSmsView/' + selectedTemplate); break;
        case tab.suppression: loadTabPartial('AlarmTemplate/SuppressionView/' + selectedTemplate); break;
        case tab.acknowledgeText: loadTabPartial('AlarmTemplate/AcknowledgeTextView/' + selectedTemplate); break;
        default: break;
    }
}

function displayLimitType(e, d) {
    e.append($("#icon-" + d.value).html());
}

function showTemplatePopup(title, edit, copy) {
    isEdit = edit;
    isCreate = !edit;
    isCopy = copy;
    console.log("template POPUIPPPP")
    let $popup = $('#templatePopup').dxPopup('instance');
    $popup.option('title', title);
    $popup.show();
}

function loadPartial(path, targetId, loadAction) {
    console.log("Path", path);
    console.log("TargetId", targetId)
    console.log("LoadAction", loadAction)
    $.ajax({ url: path, type: 'GET' })
        .done((result) => {
            $(targetId).html(result); // hewr lorte vi vores #notification Placeholder med content
            console.log('result', result)
            if (loadAction) {
               
                loadAction();
            }
        })
        .fail((e) => {
            console.log(e);
        });
}

function loadTabPartial(path) {
    loadPartial(path, '#tabularContent');
}

function templateEditShown(data) {
    loadPartial(
        isEdit
            ? 'AlarmTemplate/TemplateEditView/' + selectedTemplate
            : 'AlarmTemplate/TemplateCreateView',
        '#templateEditPlaceholder');
}

function templateEditHidden(data)
{
    isEdit = false;
    isCreate = false;
    isCopy = false;
}


function saveTemplate(data) {
    let form = $('#templateForm').dxForm('instance');
    let validation = form.validate();

    if (validation.isValid) {

        let content = form.option('formData');
        let path = isEdit
            ? 'AlarmTemplate/UpdateChannelTemplate'
            : 'AlarmTemplate/CreateChannelTemplate';

        if (isEdit && isCopy) {
            path = 'AlarmTemplate/CopyChannelTemplate';
        }

        let select = $('#templateEditSelect').dxSelectBox('instance');

        if (select) {

            let selectedType = select.option('selectedItem');

            if (selectedType) {
                if (selectedType.Key.startsWith('C_')) {
                    content.CustomUnitId = selectedType.Id;
                    content.IsCustom = true;
                }

                content.MeasurementType = selectedType.MeasurementType;
                content.DecimalPlaces = selectedType.DecimalPlaces;
                content.Units = selectedType.Units;
            }
        }

        // TODO: possible change ajax to a devextreme action so it will use the validation internally
        $.ajax({
            url: path,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(content)
        })
            .done((result, status, xhr) => {
                if (xhr.status === 200 && result.Success) {
                    let list = $('#alarmTemplateList').dxList("instance");
                    let ds = list.getDataSource();

                    list.beginUpdate();
                    loading = true;
                    ds.load()
                        .done(function (data) {
                            loading = false;
                            list.endUpdate();
                            selectedTemplate = result.Id;
                            selectTemplate(list, list.option('items'));
                            let tabs = $('#templateTabs').dxTabs('instance');
                            if (tabs) {
                                tabs.option('disabled', false);
                            }
                            isEdit = false;
                            isCreate = false;
                            isCopy = false;
                        })
                        .fail(function (error) {
                            list.endUpdate();
                            isEdit = false;
                            isCreate = false;
                            isCopy = false;
                            emsuiteToast({ message: error.message, messageType: emsuiteToastTypeEnum.Error });
                        });

                    $('#templatePopup').dxPopup('hide');
                }
            })
            .fail(e => {
                console.log(e);
                isEdit = false;
                isCreate = false;
                isCopy = false;
            });
    }
}

function deleteAlarmTemplate(text, caption, alertmessage, alertcaption) {
    $.ajax({
        url: "/AlarmTemplate/HasAlarmTemplateChannels/" + selectedTemplate,
        type: 'GET',
    }).done((result) => {
        if (result === true) {
            DevExpress.ui.dialog.alert(alertmessage, alertcaption);
        }
        else {
            DevExpress.ui.dialog.confirm(text, caption).done(function (dialogResult) {
                if (dialogResult) {
                    $.ajax({
                        url: 'AlarmTemplate/DeleteChannelTemplate/' + selectedTemplate,
                        type: 'DELETE'
                    })
                        .done((data, status, xhr) => {
                            if (xhr.status === 204 || xhr.status == 200) {
                                let list = $('#alarmTemplateList').dxList("instance");
                                let items = list.option('items');
                                let ds = list.getDataSource();

                                if (items.length > 1) {
                                    let closest = Number.MAX_SAFE_INTEGER;
                                    for (let i = 0; i < items.length; i++) {
                                        if (items[i].Id != selectedTemplate) {
                                            let dif = Math.abs(items[i].Id - selectedTemplate);
                                            if (dif < closest) {
                                                closest = items[i].Id;
                                            }
                                        }
                                    }
                                    selectedTemplate = closest;
                                } else {
                                    noTemplates();
                                }

                                list.beginUpdate();
                                ds.load()
                                    .done(function (data) {
                                        let items = list.option('items');
                                        selectTemplate(list, items);
                                        list.endUpdate();
                                        if (items.length == 0) {
                                            let tabs = $('#templateTabs').dxTabs('instance');
                                            if (tabs) {
                                                tabs.option('disabled', true);
                                            }
                                        }

                                    })
                                    .fail(function (error) {
                                        console.log(error);
                                        list.endUpdate();
                                    });
                            }
                        })
                        .fail(e => {
                            console.log(e);
                        });
                }
            });
        }
    }).fail((e) => {
        console.log(e);
    });
}

function addingSuppression(e) {
    e.appointmentData['TemplateId'] = selectedTemplate;
}

function appointmentFormOpening(e) {
    console.log(e);
}

function newThreshold(e) {
    e.data.TemplateId = selectedTemplate;
    e.data.AlarmType = alarmType.rising;
    e.data.RequiresAcknowledgement = true;
    e.data.HardwareOutput = false;
    e.data.Delay = { hours: 0, minutes: 0, seconds: 0 };
    e.data.RateOfChangeTime = { hours: 0, minutes: 10, seconds: 0 };
    e.data.AlarmSeverity = 1;
    e.data.Units = getTemplateUnits();

    thresholdEditRow = -1;
}

function thresholdBeforeSend(action, data, source) {
    if (action === 'delete' || action === 'update') {
        data.data['source'] = source;
    }
}

function setColour(setter, e) {
    setter(e.component.option('value'));
}

function initialiseColour(setter, colour) {
    colour = colour ? colour : '#FF0000';
    setter(colour);
    return colour;
}

function updateHardwareSelection(keyChar, path, selectedNodes) {
    let selectedHardware = [];

    for (var i = 0; i < selectedNodes.length; i++) {
        if (selectedNodes[i].key[0] === keyChar) {
            selectedHardware.push(
                parseInt(selectedNodes[i].key.substring(1))
            );
        }
    }

    $.ajax({
        url: '/AlarmTemplate/CheckForActiveAlarms/' + selectedTemplate,
        type: 'PUt',
        contentType: 'application/json',
        data: JSON.stringify(selectedHardware)
    }).done((result) => {
        if (result.result === true) {
            let confirmResult = DevExpress.ui.dialog.confirm(result.message, result.title);
            confirmResult.done(function (dialogResult) {
                if (dialogResult) {
                    assignChannels(path, selectedHardware)
                }
            });
        }
        else {
            assignChannels(path, selectedHardware)
        }      
    }).fail((e) => {
        emsuiteToast({ message: e, messageType: emsuiteToastTypeEnum.Error });
    });
}

function assignChannels(path, selectedHardware)
{
    $.ajax({
        url: path,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(selectedHardware)
    }).done((result) => {
        emsuiteToast({ message: result, messageType: emsuiteToastTypeEnum.Success });
    }).fail(e => {
        emsuiteToast({ message: e, messageType: emsuiteToastTypeEnum.Error });
    });
}

function saveAlarmChannels(e)
{
    var path = 'AlarmTemplate/AssignChannels/' + selectedTemplate;
    var selectedNodes = $('#channelMap').dxTreeView('instance').getSelectedNodes();
    if (selectedNodes !== undefined) {
        updateHardwareSelection('C', path, selectedNodes);
    }
}

var initialisingCheckList = false;

function initialiseCheckList(e) {
    let items = e.component.option('items');
    let selected = [];

    for (let i = 0; i < items.length; i++) {
        if (items[i].Selected) {
            selected.push(items[i]);
        }
    }

    initialisingCheckList = true;
    e.component.beginUpdate();
    e.component.option('selectedItems', selected);
    e.component.endUpdate();
    initialisingCheckList = false;
}

function smsDeviceSelectionChanged(e) {
    if (!initialisingCheckList) {
        let selected = e.component.option('selectedItems');
        let selectedDevices = [];

        for (let i = 0; i < selected.length; i++) {
            selectedDevices.push(selected[i].SerialNumber);
        }

        $.ajax({
            url: 'AlarmTemplate/AssignSmsDevices/' + selectedTemplate,
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(selectedDevices)
        }).done((result) => {
            emsuiteToast({ message: result, messageType: emsuiteToastTypeEnum.Success });
        }).fail(e => {
            emsuiteToast({ message: e, messageType: emsuiteToastTypeEnum.Error });
        });
    }
}

function beforeSend(action, data, thresholdId) {
    if (action === 'delete') {
        data.data['thresholdId'] = thresholdId;
    }
}

function insertAckNote(e, templateId) {
    e.data['TemplateId'] = templateId;
}

function beforeSendAckNote(action, data, templateId) {
    if (action === 'delete') {
        data.data['TemplateId'] = templateId;
    }
}

function showExistingActions(thresholdId) {
    $('#existingActionsPopup').dxPopup('instance').show();
}

function saveSelectedActions(e, id) {
    let grid = $('#existingActionsGrid_' + id).dxDataGrid('instance');
    let keys = grid.getSelectedRowKeys();

    $.ajax({
        url: 'AlarmTemplate/CopyActions',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ ThresholdId: id, Keys: keys })
    })
        .done((result) => {
            $('#actionGrid_' + id).dxDataGrid('instance').refresh();
            $('#existingActionsPopup').dxPopup('hide');
        })
        .fail((e) => {
            console.log(e);
            emsuiteToast({ message: e.message, messageType: emsuiteToastTypeEnum.Error });
        });
}

function setSeverityValue(e, value, rowData) {
    rowData.AlarmSeverityId = value;
}

function timeSpanFormat(span) {
    let duration = luxon.Duration.fromObject(span);

    switch (duration.days) {
        case 0: return duration.toFormat('hh:mm:ss');
        case 1: return duration.toFormat("d 'day' hh:mm:ss");
        default: return duration.toFormat("d 'days' hh:mm:ss");
    }
}

function timeSpanChange(interval, setter, e, span) {

    switch (interval) {
        case 'hour': span.hours = e.component.option('value'); break;
        case 'minute': span.minutes = e.component.option('value'); break;
        case 'second': span.seconds = e.component.option('value'); break;
        default: break;
    }

    setter(span);
}

function thresholdEditStart(e) {
    latestSeverityValue = undefined;
    thresholdEditRow = e.key;
}

function customiseThresholdElement(e, minuteText, valueFormat, valueStep) {

    const grid = $('#thresholdGrid').dxDataGrid('instance');
    let index = grid.getRowIndexByKey(thresholdEditRow);

    if (e.dataField === 'RateOfChangeTime' || e.dataField === 'HardwareOutput' || e.dataField === 'Delay') {

        let selectedAlarmType = grid.cellValue(index === -1 ? 0 : index, "AlarmType");

        switch (e.dataField) {
            case 'RateOfChangeTime': e.visible = selectedAlarmType === alarmType.rateOfChange; break;
            case 'HardwareOutput': e.visible = selectedAlarmType != alarmType.elapsed; break;
            case 'Delay': e.visible = selectedAlarmType != alarmType.elapsed && selectedAlarmType != alarmType.rateOfChange; break;
            default: break;
        }
    }

    if (e.dataField === "DisplayLimit") {

        if (grid.cellValue(index === -1 ? 0 : index, "AlarmType") === alarmType.elapsed) {

            e.label.text = e.label.text + ' (' + minuteText + ')';
            e.editorOptions.format = '#0 ' + minuteText;
            e.editorOptions.step = 1;

        } else {

            let items = $('#alarmTemplateList').dxList("instance").option('items');

            for (let i = 0; i < items.length; i++) {

                if (items[i].Id == selectedTemplate) {
                    e.label.text = e.label.text + ' (' + items[i].Units + ')';
                    e.editorOptions.format = valueFormat;
                    e.editorOptions.step = valueStep;
                    break;
                }
            }
        }
    }
}

function getTemplateUnits() {
    let items = $('#alarmTemplateList').dxList("instance").option('items');
    for (let i = 0; i < items.length; i++) {
        if (items[i].Id == selectedTemplate) {
            return items[i].Units;
        }
    }

    return '';
}

function setThresholdAlarmType(newData, value) {
    this.defaultSetCellValue(newData, value);
}

function thresholdOptionsContentReady(id, e) {
    $.ajax({ url: 'AlarmTemplate/TemplateThresholdOptions/' + id, type: 'GET' })
        .done((result) => {
            let hysteresis = $('#templateHysteresis').dxNumberBox('instance');
            hysteresis.option('value', result.HysteresisBand);
        })
        .fail((e) => {
            console.log(e);
            emsuiteToast({ message: e.statusText, messageType: emsuiteToastTypeEnum.Error });
        });
}

function saveThresholdOptions(id, messages) {
    let form = $('#thresholdOptionsForm').dxForm('instance');
    let data = form.option('formData');
    setThresholdOptions(id, data, messages);
}

function setThresholdOptions(id, data, messages) {
    $.ajax({
        url: 'AlarmTemplate/TemplateThresholdOptions/' + id,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data)
    })
        .done(() => {
            emsuiteToast({ message: messages.success, messageType: emsuiteToastTypeEnum.Success });
        })
        .fail((e) => {
            console.log(e.responseText);
            emsuiteToast({ message: messages.failed + e.statusText, messageType: emsuiteToastTypeEnum.Error });
        });
}

function hysteresisChange(e) {
    let saveBtn = $('#SaveLimitSettings').dxButton('instance');
    saveBtn.option("disabled", false);

    let form = $('#thresholdOptionsForm').dxForm('instance');
    let edit = $('#templateHysteresis').dxNumberBox('instance');
    form.updateData('HysteresisBand', e.value);
}

function severityCellTemplate(e, c) {
    e.append(
        '<div class="colour-block" style="background-color:'
        + getSeverityColour(c.data.AlarmSeverity) + '">'
        + getSeverityLabel(c.data.AlarmSeverity)
        + '</div>');
}

function initialiseSeverity(e) {
    if (e.element[0]) {
        let value = e.component.option('value');
        setSeverityClass(e.element[0], value);
        setSeveritySliderLabel(value);
    }
}

var oldSeverityValue = 1;
function initialiseSeverityValue(value) {
    if (value) {
        oldSeverityValue = value;
        return 5 - value;
    }

    return 4;
}

function getSeveritySliderLabel(value) {
    return getSeverityLabel(5 - value);
}

let latestSeverityValue = undefined;
function severityChanged(e, setter) {
    setSeverityClass(e.element[0], e.value);
    setSeveritySliderLabel(e.value);
    latestSeverityValue = 5 - e.value;

    if (!e.component.__clearTimeout) {
        e.component.__clearTimeout = (function () {
            clearTimeout(this.__timeout);
            this.__timeout = null;
        }).bind(e.component);
    }
    if (e.component.__timeout) {
        e.component.__clearTimeout();
    }
    e.component.__timeout = setTimeout(() => {
        e.component.__clearTimeout();
        setter(5 - e.value);
    }, 500);
}

function onThresholdRowUpdating(e) {
    if (latestSeverityValue !== undefined) {
        if (e.newData.AlarmSeverity === undefined) {
            e.newData.AlarmSeverity = 'AlarmSeverity';
            e.newData.AlarmSeverity = latestSeverityValue;
        }
        else
            e.newData.AlarmSeverity = latestSeverityValue;
    }
    latestSeverityValue = undefined;
}

function setSeveritySliderLabel(value) {
    document.getElementById('severitySliderLabel').innerText = getSeveritySliderLabel(value);
}

function setSeverityClass(element, severity) {
    if (element && severity) {
        element.classList.remove('warning');
        element.classList.remove('minor');
        element.classList.remove('major');
        element.classList.remove('critical');

        switch (severity) {
            case 1: element.classList.add('warning'); break;
            case 2: element.classList.add('minor'); break;
            case 3: element.classList.add('major'); break;
            default: element.classList.add('critical'); break;
        }
    }
}

function setColourFromSeverity(e, value, row) {
    e.AlarmSeverity = value;
    if (row.Colour === getSeverityColour(oldSeverityValue)) {
        e.Colour = getSeverityColour(value);
        oldSeverityValue = value;
    }
}

function toggleLocationMode(e) {
    if (e.value != e.previousValue) {
        if (e.value) {
            document.querySelector('.location-all').classList.add('d-none');
            document.querySelector('.location-specific').classList.remove('d-none');
        } else {
            document.querySelector('.location-all').classList.remove('d-none');
            document.querySelector('.location-specific').classList.add('d-none');
        }
    }
}

function OnTreeViewItemClick(e) {
    if (!e.itemData.IsCheckBoxEnabled) {
        e.event.preventDefault();
    }
}

function OnTreeViewItemRendered(e) {
    if (!e.itemData.IsCheckBoxEnabled) {
        e.itemElement.parent().find(".dx-checkbox").dxCheckBox("instance").option("disabled", true);
    }
}

let thresholdId = 0;
function showNotificationPopup(e, titlePrefix) {
    thresholdId = e.row.data.Id;

    loadPartial('AlarmTemplate/NotificationView/' + thresholdId, '#notificationPlaceholder');

    let title = titlePrefix + e.row.cells[0].displayValue + ' ' + e.row.data.DisplayLimitText;
    let $popup = $('#notificationPopup').dxPopup('instance');
    $popup.option('title', title);
    $popup.show();
}

function saveNotifications() {

    saveThresholdNotification(thresholdId);
    $('#notificationPopup').dxPopup('hide');
}

function saveAndCopyNotifications() {

    const grid = $('#thresholdGrid').dxDataGrid('instance');
    const rows = grid.getVisibleRows();

    for (var i = 0; i < rows.length; i++) {
        saveThresholdNotification(rows[i].data.Id);
    }

    $('#notificationPopup').dxPopup('hide');
}

function saveThresholdNotification(id) {
    const nodeView = saveNotificationSelectionMode(id);

    if (nodeView) {
        saveSelectedNotificationNodes(id);
    } else {
        saveSelectedNotificationUsers(id);
    }
}

function notificationShown(e) {
    //console.log(e);
}

function notificationHidden(e) {
    //console.log(e);
}

function saveNotificationSelectionMode(thresholdId) {

    const toggle = $('#toggleLocationSwitch').dxSwitch('instance');
    const value = toggle.option('value');

    $.ajax({
        url: 'AlarmTemplate/SetLocationSpecificMode/' + thresholdId + '?mode=' + value,
        type: 'PUT'
    });

    return value;
}

function saveSelectedNotificationNodes(thresholdId) {

    // Get the instance of the NumberBoxes and SelectBox
    const roundRobinHour = $("#roundRobinHour").dxNumberBox("instance");
    const roundRobinMin = $("#roundRobinMin").dxNumberBox("instance");
    const roundRobinSec = $("#roundRobinSec").dxNumberBox("instance");
    const genderSelectBox = $("#genderSelectBox").dxSelectBox("instance");

    // Get the values from the NumberBoxes and SelectBox
    const hourValue = roundRobinHour.option("value");
    const minValue = roundRobinMin.option("value");
    const secValue = roundRobinSec.option("value");
    const genderValue = genderSelectBox.option("value");

    // Log the values to the console
    console.log("Hour:", hourValue);
    console.log("Minute:", minValue);
    console.log("Second:", secValue);
    console.log("Gender:", genderValue);

    const tree = $("#notificationTree").dxTreeView("instance");
    const selected = tree.getSelectedNodes();

    let notificationPackage = {
        roundRobinInterval: {
            seconds: secValue,
            minutes: minValue,
            hours: hourValue
        },
        gender: genderValue,
        selectedUsers: []
    }

    for (let i = 0; i < selected.length; i++) {
        if (selected[i].key.startsWith('U')) {
            let parts = selected[i].key.split('_');
           notificationPackage.selectedUsers.push({ UserId: parts[1], ZoneId: parts[2] });
        }
    }

    $.ajax({
        url: 'AlarmTemplate/AssignUserNodes/' + thresholdId,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(notificationPackage)
    }).done((result) => {
        emsuiteToast({ message: result, messageType: emsuiteToastTypeEnum.Success });
    }).fail(e => {
        emsuiteToast({ message: e, messageType: emsuiteToastTypeEnum.Error });
    });
}

function saveSelectedNotificationUsers(thresholdId) {

    // Get the instance of the NumberBoxes and SelectBox
    const roundRobinHour = $("#roundRobinHour").dxNumberBox("instance");
    const roundRobinMin = $("#roundRobinMin").dxNumberBox("instance");
    const roundRobinSec = $("#roundRobinSec").dxNumberBox("instance");
    const genderSelectBox = $("#genderSelectBox").dxSelectBox("instance");

    // Get the values from the NumberBoxes and SelectBox
    const hourValue = roundRobinHour.option("value");
    const minValue = roundRobinMin.option("value");
    const secValue = roundRobinSec.option("value");
    const genderValue = genderSelectBox.option("value");

    // Log the values to the console
    console.log("Hour:", hourValue);
    console.log("Minute:", minValue);
    console.log("Second:", secValue);
    console.log("Gender:", genderValue);


    const list = $("#notificationList").dxList("instance");
    const selected = list.option('selectedItems');

    let notificationPackage = {
        roundRobinInterval: {
            seconds: secValue,
            minutes: minValue,
            hours: hourValue
        },
        gender: genderValue,
        selectedUsers: []
    }


    for (let i = 0; i < selected.length; i++) {
        notificationPackage.selectedUsers.push(selected[i].UserId);
    }

    $.ajax({
        url: 'AlarmTemplate/AssignUsers/' + thresholdId,
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(notificationPackage)
    }).done((result) => {
        emsuiteToast({ message: result, messageType: emsuiteToastTypeEnum.Success });
    }).fail(e => {
        emsuiteToast({ message: e, messageType: emsuiteToastTypeEnum.Error });
    });
}


