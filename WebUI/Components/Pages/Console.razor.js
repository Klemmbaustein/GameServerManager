window.consoleScrollDown = function () {
	console.log("test");
	var consoleScroll = document.getElementById("console");
	consoleScroll.scrollTop = consoleScroll.scrollHeight;
}