<?php
include 'connection.php';
error_reporting(0);
try {
	$sid=htmlspecialchars($_GET['sessionid']); //sessie id uit url sanitizen
	if($sid == NULL){
		$jsonError = new \stdClass();
		$jsonError->id = 0;
		$myJSON = json_encode($jsonError);
		echo $myJSON;
		return;
	}
} catch (\Exception $e) {
	$jsonError = new \stdClass();
	$jsonError->id = 0;
	$myJSON = json_encode($jsonError);
	echo $myJSON;
	return;
}


session_id($sid); //sessie id voor deze sessie instellen naar wat uit url kwam
session_start();
if($_SESSION == NULL){
	$jsonError = new \stdClass();
	$jsonError->id = 0;
	$myJSON = json_encode($jsonError);
	echo $myJSON;
}else{
	if ($stmt = $con->prepare('SELECT id, password, displayname FROM users WHERE naam = :username')) {
		$stmt->bindParam(':username', $_GET['username']);
		$stmt->execute();
		$row = $stmt->fetch();
		if ($stmt->rowCount() > 0) {
			if (password_verify($_GET['password'], $row[1])) {
				session_regenerate_id();
				$_SESSION['loggedin'] = TRUE;
				$_SESSION['name'] = $_GET['username'];
				$_SESSION['id'] = $row[0];

				$json = new \stdClass();
				$json->name = $_SESSION['name'];
				$json->id = $_SESSION['id'];
				$json->displayname = $row['displayname'];
				$myJSON = json_encode($json);
				echo $myJSON;
			} else {
				$jsonError = new \stdClass();
				$jsonError->id = 0;
				$myJSON = json_encode($jsonError);
				echo $myJSON;

			}
		} else {
			$jsonError = new \stdClass();
			$jsonError->id = 0;
			$myJSON = json_encode($jsonError);
			echo $myJSON;
			return;
		}
	}
}
?>
