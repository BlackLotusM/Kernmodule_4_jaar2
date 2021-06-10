<?php
/* Connect to a MySQL database using driver invocation */

try {
  $dsn = 'mysql:dbname=mikeywoudstra;host=127.0.0.1';
  $user = '';
  $password = '';
  $con = new PDO($dsn, $user, $password);
} catch (PDOException $e) {
    echo 'Connection failed';
}
?>
