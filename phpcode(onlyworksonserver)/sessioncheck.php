<?php
include 'connection.php';
if (isset($_GET['sessionid'])) { //staat de sessie id in de url?
  //$pass = preg_replace("/\s+/", "", $_GET['password']);
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
  $json = new \stdClass();
  $json->id = 1;
  $json->serverID = $_SESSION['serverID'];
  $json->sessionID = $_SESSION['sessionID'];
  $json = json_encode($json);
  echo $json;
}
}
?>
