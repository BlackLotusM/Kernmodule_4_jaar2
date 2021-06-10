<?php

session_start();
include 'connection.php';

//Validate id int
if (filter_var( $_GET['id'], FILTER_VALIDATE_INT)) {
	if ($stmt = $con->prepare('SELECT password, game_naam FROM game WHERE id = :id')) {
		//Validate password
		$pass = preg_replace("/\s+/", "", $_GET['password']);
		$stmt->bindParam(':id', $_GET['id']);
		$stmt->execute();
		$row = $stmt->fetch();

		if (password_verify($pass, $row['password'])) {
			$_SESSION['sessionID'] = session_id();
			$_SESSION['serverID'] = $_GET['id'];

			$json = new \stdClass();
			$json->id = 1;
			$json->session =  session_id();
			$myJSON = json_encode($json);

			echo $myJSON;
		}else{
			StatusZero();
		}
	}else {
		StatusZero();
	}
}else {
	StatusZero();
}

function StatusZero() {
	$jsonError = new \stdClass();
	$jsonError->id = 0;
	$myJSON = json_encode($jsonError);
	echo $myJSON;
}
?>
