<?php
include 'connection.php';
error_reporting(0);

$sid=htmlspecialchars($_GET['sessionid']); //sessie id uit url sanitizen
if($sid == NULL){
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
}else
if ($stmt = $con->prepare('SELECT id, score FROM highscore WHERE user_id = :id')) {
  $stmt->bindParam(':id', $_GET['id']);
  $stmt->execute();
  $row = $stmt->fetch();
  if ($stmt->rowCount() > 0) {
    $score = $row['score'] + $_GET['score'];
    if ($stmt = $con->prepare('UPDATE highscore SET score = :test WHERE user_id = :id')) {
      $stmt->bindParam(':test', $score);
      $stmt->bindParam(':id', $_GET['id']);
      $stmt->execute();
      $jsonError = new \stdClass();
      $jsonError->id = 1;
      $myJSON = json_encode($jsonError);
      echo $myJSON;
      return;
    }
  }else {
    $sql = "INSERT INTO highscore (user_id, score, game_id) VALUES (?,?,?)";
    $con->prepare($sql)->execute([$_GET['id'], $_GET['score'], 1]);
    $jsonError = new \stdClass();
    $jsonError->id = 0;
    $myJSON = json_encode($jsonError);
    echo $myJSON;
    return;
  }
}
?>
