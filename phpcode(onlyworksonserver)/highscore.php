<?php
include 'connection.php';
if (isset($_GET['sessionid'])) { //staat de sessie id in de url?
  //$pass = preg_replace("/\s+/", "", $_GET['password']);
  if($_GET['sessionid'] == null){
    StatusZero();
    return;
  }
  $sid=htmlspecialchars(preg_replace("/\s+/", "",$_GET['sessionid'])); //sessie id uit url sanitizen
  session_id($sid); //sessie id voor deze sessie instellen naar wat uit url kwam
  session_start();

  if($_SESSION == NULL){
    $json = new \stdClass();
    $json->id = 0;
    $json = json_encode($json);
    echo $json;
    echo "Session does not exist.";
  }else{
    //SELECT highscore.score, users.naam
  //FROM highscore
 //INNER JOIN users
    //ON highscore.user_id = users.id;

    if (filter_var( $_GET['amount'], FILTER_VALIDATE_INT)) {
      $amount = $_GET["amount"];
    	if ($stmt = $con->prepare("SELECT highscore.score, users.naam FROM highscore INNER JOIN users ON highscore.user_id = users.id LIMIT $amount")) {
    		//$stmt->bindParam(':temp', 2);
    		$stmt->execute();

        $rows = array();
          while($r = $stmt->fetch(PDO::FETCH_ASSOC)) {
            $rows[] = $r;
          }

        print json_encode($rows);


      }else{
    			StatusZero();
    		}
    	}else {
    		StatusZero();
    	}
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
