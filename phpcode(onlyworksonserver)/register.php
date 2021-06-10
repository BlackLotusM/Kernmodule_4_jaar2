<?php
include 'connection.php';
if ( !isset($_GET['username'], $_GET['password'], $_GET['displayname']) ) {
	$jsonError = new \stdClass();
	$jsonError->id = 0;
	$myJSON = json_encode($jsonError);
	echo $myJSON;
	return;
}

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
	$stmt = $con->prepare('SELECT id, password, displayname FROM users WHERE naam = :username');
	$stmt->bindParam(':username', $_GET['username']);
	$stmt->execute();
	$row = $stmt->fetch();

	if($row == NULL){

		$sql = "INSERT INTO users (naam, displayname, password) VALUES (?,?,?)";
		$con->prepare($sql)->execute([$_GET['username'], $_GET['displayname'], password_hash($_GET['password'], PASSWORD_DEFAULT)]);

		if ($stmt = $con->prepare('SELECT id FROM users WHERE naam = :username')) {
			$stmt->bindParam(':username', $_GET['username']);
			$stmt->execute();
			$row = $stmt->fetch();
			if ($stmt->rowCount() > 0) {
				$sql = "INSERT INTO highscore (user_id, score, game_id) VALUES (?,?,?)";
				$con->prepare($sql)->execute([$row['id'], 0, 1]);
			}
		}
		$jsonError = new \stdClass();
		$jsonError->id = 1;
		$myJSON = json_encode($jsonError);
		echo $myJSON;
		return;
	}else{
		$jsonError = new \stdClass();
		$jsonError->id = 0;
		$myJSON = json_encode($jsonError);
		echo $myJSON;
	}
}
?>
