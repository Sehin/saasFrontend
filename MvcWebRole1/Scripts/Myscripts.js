                        
function deleteAcc(funcAccId) {
    $.get("/Social/deleteAcc", { accId: funcAccId })
    $.ajax({
        success: function (result) {
            reloadView();
        }
    });

}
function addGr(funcSocId) {

    $.get("/Social/addGroup", { socId: funcSocId, groupId: $('#groupId').val() })
    $.ajax({
        success: function (result) {
            reloadView();
        }
    });

}
function reloadView() {
    $('#FbSocInfo').load("/Tool/GetFbSocInfoPartial");
}
