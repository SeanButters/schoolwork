using Npgsql;
using System;


class Program
{

    //Returns total price of order
    private static double getTotalPrice(NpgsqlConnection connection, string order_id)
    {
        double totalPrice = 0.00;
        connection.Open();
        using (NpgsqlCommand cmd = new NpgsqlCommand("select Sum(price * quantity) from order_items inner join product on product.product_id = order_items.product_id where order_id = " + order_id + ";", connection))
        {
            using (NpgsqlDataReader reader = cmd.ExecuteReader())
            {
                reader.Read();
                if(!reader.IsDBNull(0)) { totalPrice = reader.GetDouble(0); }
            }
        }
        connection.Close();
        return totalPrice;
    }

    //Displays items and quantity of current order
    private static void viewOder(NpgsqlConnection connection, string order_id)
    {
        Console.WriteLine("Current Order:\n");

        // Get products in current order
        connection.Open();
        using (NpgsqlCommand cmd = new NpgsqlCommand("select product.product_id, name, quantity, product.price * quantity as item_price from order_items inner join product on product.product_id = order_items.product_id where order_id = "+order_id+";", connection))
        {
            using (NpgsqlDataReader reader = cmd.ExecuteReader())
            {
                Console.WriteLine("{0,-5} {1,-20} {2,-8} {3,-8}", "ID", "Name", "Quantity", "Price");
                Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");

                while (reader.Read())
                {
                    string productId = reader[0].ToString();
                    string name = reader[1].ToString();
                    string quantity = reader[2].ToString();
                    string price = reader[3].ToString();

                    Console.WriteLine("{0,-5} {1,-20} {2,-8} {3, -8}", productId, name, quantity, price);
                }
            }
        }
        connection.Close();
        Console.WriteLine("Total Price: $"+getTotalPrice(connection, order_id).ToString());
    }

    //Displays list of products for the customer
    private static void ProductCatalog(NpgsqlConnection connection)
    {
        Console.WriteLine("Product Catalog:\n");

        // Get All products
        connection.Open();
        using (NpgsqlCommand cmd = new NpgsqlCommand("SELECT product_id, name, price, description, category FROM product ORDER BY category", connection))
        {
            using (NpgsqlDataReader reader = cmd.ExecuteReader())
            {
                Console.WriteLine("{0,-5} {1,-20} {2,-8} {3,-60} {4, -20}", "ID", "Name", "Price", "Description", "Category");
                Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");

                while (reader.Read())
                {
                    string productId = reader[0].ToString();
                    string name = reader[1].ToString();
                    string description = reader[2].ToString();
                    string price = reader[3].ToString();
                    string category = reader[4].ToString();

                    Console.WriteLine("{0,-5} {1,-20} {2,-8} {3,-60} {4, -20}", productId, name, description, price, category);
                }
            }
        }
        connection.Close();
    }

    //Shopping cart. Create and place an order
    public static void shop(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        //Create new order
        Console.WriteLine("Create a new order\n");
        connection.Open();
        using NpgsqlCommand cmd = new NpgsqlCommand("select order_id FROM orders;", connection);
        using NpgsqlDataReader reader = cmd.ExecuteReader();
        int order_id = 1;
        while (reader.Read())
        {
            order_id++;
        }
        connection.Close();

        //Add credit card
        connection.Open();
        using NpgsqlCommand cmd1 = new NpgsqlCommand("select card_number FROM CreditCard WHERE customer_id = " + ID + ";", connection);
        using NpgsqlDataReader reader1 = cmd1.ExecuteReader();
        reader1.Read();
        if (!reader1.HasRows)
        {
            Console.WriteLine("Error: No credit card availible. Please insert a new credit card.");
            Console.ReadLine();
            connection.Close();
            user(connection, ID);
            return;
        }
        string card_number = reader1[0].ToString();
        connection.Close();

        connection.Open();
        using NpgsqlCommand cmd2 = new NpgsqlCommand("INSERT INTO orders (order_id, status, credit_card_number, total_price) VALUES (" + order_id.ToString() + ", 'incomplete', '" + card_number + "', 0);", connection);
        int rowsAffected = cmd2.ExecuteNonQuery();
        connection.Close();

        Console.WriteLine("Enter a product ID to add to cart\nEnter 'view catalog' to view products\nEnter 'view order' to view current order\nEnter 'edit' to edit order\nEnter 'submit' to complete order\nEnter 'back' or 'cancel' to cancel current order");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while(input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if(input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if (input == "back" || input == "cancel")
            {
                //Delete current order if canceled
                connection.Open();
                using NpgsqlCommand cmd3 = new NpgsqlCommand("DELETE FROM orders WHERE order_id = " + order_id.ToString() + ";", connection);
                rowsAffected = cmd3.ExecuteNonQuery();
                connection.Close();
                user(connection, ID);
                break;
            }
            else if (input == "view catalog" || input == "view shop" ||input == "view products")
            {
                ProductCatalog(connection);
                Console.WriteLine("\nInput:");
            }
            else if(input == "view order")
            {
                viewOder(connection, order_id.ToString());
                Console.WriteLine("\nInput:");
            }
            else if(int.TryParse(input, out temp))
            {
                //Check if valid product id
                connection.Open();
                using NpgsqlCommand cmd5 = new NpgsqlCommand("SELECT product_id FROM product where product_id = " + input + ";", connection);
                using NpgsqlDataReader reader5 = cmd5.ExecuteReader();
                reader5.Read();
                if (!reader5.HasRows)
                {
                    Console.WriteLine("Unknown product ID or Unknown command. Please try again:");
                    connection.Close();
                    continue;
                }
                connection.Close();

                //Check if item in cart
                connection.Open();
                using NpgsqlCommand cmd6 = new NpgsqlCommand("SELECT product_id FROM order_items where product_id = " + input + " AND order_id = "+order_id+";", connection);
                using NpgsqlDataReader reader6 = cmd6.ExecuteReader();
                reader6.Read();
                if (reader6.HasRows)
                {
                    Console.WriteLine("Product already in cart. Please try again or use edit to edit items in cart:");
                    connection.Close();
                    continue;
                }
                connection.Close();

                //Ask for quantity
                Console.WriteLine("Enter quantity:");
                string input2 = Console.ReadLine();
                int temp2;
                //check quantity is an int
                while (!int.TryParse(input2, out temp2))
                {
                    Console.WriteLine("Enter quantity as an int:");
                    input2 = Console.ReadLine();
                }

                connection.Open();
                using NpgsqlCommand cmd7 = new NpgsqlCommand("SELECT name FROM product where product_id = " + input + ";", connection);
                using NpgsqlDataReader reader7 = cmd7.ExecuteReader();
                reader7.Read();
                string product_name = reader7[0].ToString();
                connection.Close();

                connection.Open();
                using NpgsqlCommand cmd8 = new NpgsqlCommand("INSERT INTO order_items (order_id, product_id, quantity) VALUES (" + order_id.ToString() + ", " + input + ", " + input2 + ");", connection);
                rowsAffected = cmd8.ExecuteNonQuery();
                connection.Close();
                Console.WriteLine("Successfully added "+input2+" "+product_name+" to the cart.\n");
            }
            else if(input == "edit" || input == "edit order")
            {
                Console.WriteLine("\nEnter product ID to edit quantity\nEnter delete to remove a product from cart\nEnter view to view current order\nEnter back to return to cart");
                Console.WriteLine("\nInput:");

                while(input != "back" || input != "return")
                {
                    input = Console.ReadLine();
                    input = input.ToLower();

                    if(input == "view" || input == "view order")
                    {
                        viewOder(connection, order_id.ToString());
                        Console.WriteLine("\nEnter product ID to edit quantity\nEnter delete to remove a product from cart\nEnter view to view current order\nEnter back to return to cart");
                        Console.WriteLine("\nInput:");
                    }
                    else if(input == "delete" || input == "remove")
                    {
                        Console.WriteLine("Enter item id to delete: ");
                        string input2 = "A";
                        while (true)
                        {
                            input2 = Console.ReadLine();

                            if(!int.TryParse(input2, out temp))
                            {
                                Console.WriteLine("Enter product id as an int:");
                                continue;
                            }

                            connection.Open();
                            using NpgsqlCommand cmd12 = new NpgsqlCommand("SELECT product_id FROM order_items where product_id = " + input2 + " AND order_id = " + order_id + ";", connection);
                            using NpgsqlDataReader reader12 = cmd12.ExecuteReader();
                            reader12.Read();
                            if (reader12.HasRows)
                            {
                                connection.Close();
                                break;
                            }
                            connection.Close();
                            Console.WriteLine("Product ID not in cart. Try Again");
                        }

                        connection.Open();
                        using NpgsqlCommand cmd11 = new NpgsqlCommand("DELETE FROM order_items where order_id  = " + order_id + " AND product_id = " + input2 + ";", connection);
                        rowsAffected = cmd11.ExecuteNonQuery();
                        connection.Close();
                        Console.WriteLine("Item removed from cart");
                        break;
                    }
                    else if (int.TryParse(input, out temp))
                    {
                        connection.Open();
                        using NpgsqlCommand cmd9 = new NpgsqlCommand("SELECT product_id FROM order_items where product_id = " + input + " AND order_id = " + order_id + ";", connection);
                        using NpgsqlDataReader reader9 = cmd9.ExecuteReader();
                        reader9.Read();
                        if (!reader9.HasRows)
                        {
                            connection.Close();
                            Console.WriteLine("Unkown command/product ID. Please try again:");
                            continue;
                        }
                        connection.Close();

                        Console.WriteLine("Enter new quantity: ");
                        string input2 = Console.ReadLine();

                        while(!int.TryParse(input2, out temp))
                        {
                            Console.WriteLine("Enter new quantity as an int: ");
                            input2 = Console.ReadLine();
                        }

                        connection.Open();
                        using NpgsqlCommand cmd10 = new NpgsqlCommand("Update order_items SET quantity = " + input2 + " where order_id  = " + order_id + " AND product_id = " + input + ";", connection);
                        rowsAffected = cmd10.ExecuteNonQuery();
                        connection.Close();
                        Console.WriteLine("Quantity updated");

                        break;
                    }
                    else
                    {
                        Console.WriteLine("Unkown command/product ID. Please try again");
                    }
                }
                Console.WriteLine("\nEnter a product ID to add to cart\nEnter 'view catalog' to view products\nEnter 'view order' to view current order\nEnter 'edit' to edit order\nEnter 'submit' to complete order\nEnter 'back' or 'cancel' to cancel current order");
                Console.WriteLine("\nInput:");
            }
            else if(input == "help")
            {
                Console.WriteLine("Enter a product ID to add to cart\nEnter 'view catalog' to view products\nEnter 'view order' to view current order\nEnter 'edit' to edit order\nEnter 'submit' to complete order\nEnter 'back' or 'cancel' to cancel current order");
                Console.WriteLine("\nInput:");
            }
            else if(input == "submit")
            {
                //Submit order
                string totalPrice = getTotalPrice(connection, order_id.ToString()).ToString();
                Console.Clear();
                Console.WriteLine("Total price: is $"+totalPrice);
                Console.WriteLine("Enter delivery plan (standard, express)\nOr enter 'back' to return to order");
                Console.WriteLine("\nInput");

                string input2 = "";

                while (input2 != "back")
                {
                    input2 = Console.ReadLine();
                    input2 = input2.ToLower();

                    if (input2 == "standard")
                    {
                        //Standard delivery
                        connection.Open();
                        using NpgsqlCommand cmd13 = new NpgsqlCommand("INSERT INTO delivery (delivery_id, order_id, type) VALUES (" + order_id.ToString() + ", " + order_id.ToString() + ", 'standard');", connection);
                        rowsAffected = cmd13.ExecuteNonQuery();
                        connection.Close();

                        //Update order status and user balance
                        connection.Open();
                        using NpgsqlCommand cmd14 = new NpgsqlCommand("Update orders SET total_price = " + totalPrice + " where order_id  = " + order_id + ";", connection);
                        rowsAffected = cmd14.ExecuteNonQuery();
                        connection.Close();

                        connection.Open();
                        using NpgsqlCommand cmd15 = new NpgsqlCommand("Update customer SET balance = balance - " + totalPrice + " where customer_id  = " + ID+ ";", connection);
                        rowsAffected = cmd15.ExecuteNonQuery();
                        connection.Close();

                        connection.Open();
                        using NpgsqlCommand cmd16 = new NpgsqlCommand("Update orders SET status = 'complete' where order_id  = " + order_id + ";", connection);
                        rowsAffected = cmd16.ExecuteNonQuery();
                        connection.Close();


                        Console.WriteLine("Order complete");
                        Console.ReadLine();

                        user(connection, ID);
                        return;
                    }
                    else if (input2 == "express")
                    {
                        //Express delivery
                        connection.Open();
                        using NpgsqlCommand cmd13 = new NpgsqlCommand("INSERT INTO delivery (delivery_id, order_id, type) VALUES (" + order_id.ToString() + ", " + order_id.ToString() + ", 'express');", connection);
                        rowsAffected = cmd13.ExecuteNonQuery();
                        connection.Close();

                        //Update order status and user balance
                        connection.Open();
                        using NpgsqlCommand cmd14 = new NpgsqlCommand("Update orders SET total_price = " + totalPrice + " where order_id  = " + order_id + ";", connection);
                        rowsAffected = cmd14.ExecuteNonQuery();
                        connection.Close();

                        connection.Open();
                        using NpgsqlCommand cmd15 = new NpgsqlCommand("Update customer SET balance = balance - " + totalPrice + " where customer_id  = " + ID + ";", connection);
                        rowsAffected = cmd15.ExecuteNonQuery();
                        connection.Close();

                        connection.Open();
                        using NpgsqlCommand cmd16 = new NpgsqlCommand("Update orders SET status = 'complete' where order_id  = " + order_id + ";", connection);
                        rowsAffected = cmd16.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("Order complete");
                        Console.ReadLine();
                        user(connection, ID);
                        return;
                    }
                    else if (input2 == "cancel")
                    {
                        input2 = "back";
                    }
                    else
                    {
                        Console.WriteLine("Unkown delivery plan.");
                        Console.WriteLine("Enter delivery plan (standard, express)\nOr enter 'back' to return to order");
                    }
                }

                Console.Clear() ;
                Console.WriteLine("Enter a product ID to add to cart\nEnter 'view catalog' to view products\nEnter 'view order' to view current order\nEnter 'edit' to edit order\nEnter 'submit' to complete order\nEnter 'back' or 'cancel' to cancel current order");
                Console.WriteLine("\nInput:");
            }
            else
            {
                Console.WriteLine("Unkown command/product ID. Please try again");
            }

        }
        //Delete unifinished orders
        connection.Open();
        using NpgsqlCommand cmd4 = new NpgsqlCommand("DELETE FROM orders WHERE status = 'incomplete';", connection);
        rowsAffected = cmd4.ExecuteNonQuery();
        connection.Close();
    }


    //Display list of customer credit cards
    public static void displayCard(NpgsqlConnection connection, string ID)
    {
        connection.Open();
        using NpgsqlCommand cmd = new NpgsqlCommand("select credit_card_id, card_number, payment_address FROM CreditCard WHERE customer_id = " + ID + " ORDER BY credit_card_id;", connection);

        using NpgsqlDataReader reader = cmd.ExecuteReader();
        Console.WriteLine("ID\tCredit Card Number\t\tPayment Address");
        Console.WriteLine("---------------------------------------------------------------------------------");
        while (reader.Read())
        {
            Console.WriteLine(reader[0] + "\t" + reader[1] + "\t\t" + reader[2]);
        }
        connection.Close();
    }

    //Add new credit card to customer
    public static void addCard(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nAdd a new Credit Card");
        Console.WriteLine("Enter new credit card number\nAlternativley enter 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back")
            {
                products(connection, ID);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Enter payment address:");
                string input2 = Console.ReadLine();

                //Get next availible ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("select credit_card_id FROM creditcard WHERE customer_id  = " + ID + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                int i = 1;
                while (reader.Read())
                {
                    i++;
                }
                connection.Close();

                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("INSERT INTO creditcard (credit_card_id, customer_id, card_number, payment_address) VALUES (" + i.ToString() + ", " + ID + ", '" + input + "', '"+input2+"');", connection);
                int rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();

                creditCards(connection, ID);
                break;
            }
        }
    }

    //Modify an existing credit card
    public static void modifyCard(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nModify an Credit Card");
        Console.WriteLine("Enter an credit card ID to modify\nAlternativley enter 'view' to view current addresses or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (int.TryParse(input, out temp))
            {
                //Check if valid ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("select credit_card_id FROM creditcard WHERE customer_id  = " + ID + " AND credit_card_id = " + input + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Unknown credit card ID");
                    connection.Close();
                    continue;
                }
                connection.Close();

                Console.WriteLine("Enter what field you wish to update (card number, payment address):");
                string input2 = "";
                while (input2 != "back")
                {
                    input2 = Console.ReadLine();
                    input2 = input2.ToLower();
                    if (input2 == "back")
                    {
                        break;
                    }
                    else if(input2 == "card number" || input2 == "number")
                    {
                        //Update card number
                        Console.WriteLine("Enter updated card number");
                        string card_number = Console.ReadLine();
                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update creditcard SET card_number = '" + card_number + "' where customer_id  = " + ID + " AND credit_card_id = " + input + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();
                        Console.WriteLine("Address successfully updated");
                        Console.ReadLine();

                        creditCards(connection, ID);
                        break;
                    }
                    else if (input2 == "payment address" || input2 == "address")
                    {
                        //Update payment address
                        Console.WriteLine("Enter updated payment address");
                        string payment_address = Console.ReadLine();
                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update creditcard SET payment_address = '" + payment_address + "' where customer_id  = " + ID + " AND credit_card_id = " + input + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();
                        Console.WriteLine("Address successfully updated");
                        Console.ReadLine();

                        creditCards(connection, ID);
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Invalid field. Try again:");
                    }
                }
            }
            else if (input == "back")
            {
                creditCards(connection, ID);
                break;
            }
            else if (input == "view")
            {
                //dispay credit card information
                displayCard(connection, ID);
                Console.WriteLine("Enter an credit card ID to modify\nAlternativley enter 'view' to view current addresses or 'back' to return");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter address ID as an int. Please Try Again:");
            }
        }
    }

    //Remove a credit card
    public static void removeCard(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nRemove an credit card");
        Console.WriteLine("Enter a credit card ID to delete\nAlternativley enter 'view' to view current credit cards or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (int.TryParse(input, out temp))
            {
                //Check if valid ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("select credit_card_id FROM creditcard WHERE customer_id  = " + ID + " AND credit_card_id = " + input + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Unknown credit card ID");
                    connection.Close();
                    continue;
                }
                connection.Close();

                //Delete credit card
                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("DELETE FROM creditcard WHERE credit_card_id = " + input + " AND customer_id = " + ID + ";", connection);
                int rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
                Console.WriteLine("Credit card successfully deleted");
                Console.ReadLine();

                creditCards(connection, ID);
                break;
            }
            else if (input == "back")
            {
                creditCards(connection, ID);
                break;
            }
            else if (input == "view")
            { 
                //Display credit cards
                displayCard(connection, ID);
                Console.WriteLine("Enter a credit card ID to delete\nAlternativley enter 'view' to view current credit cards or 'back' to return");

            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter credit card ID as an int. Please Try Again:");
            }
        }
    }
    public static void creditCards(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("View and edit addresses");
        Console.WriteLine("\nEnter 'view' to view list of credit cards\nEnter 'add' to add a new credit card\nEnter 'delete' to delete a credit card\nEnter 'modify' to modify a credit card\nEnter 'back' to return or 'logout' to logout");
        Console.WriteLine("\nInput:");
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back")
            {
                user(connection, ID);
                break;
            }
            else if (input == "logout")
            {
                loginUser(connection);
                break;
            }
            else if (input == "add")
            {
                addCard(connection, ID);
                break;
            }
            else if (input == "remove" || input == "delete")
            {
                removeCard(connection, ID);
                break;
            }
            else if (input == "modify")
            {
                modifyCard(connection, ID);
                break;
            }
            else if (input == "view")
            {
                displayCard(connection, ID);
                Console.WriteLine("\nEnter 'view' to view list of credit cards\nEnter 'add' to add a new credit card\nEnter 'delete' to delete a credit card\nEnter 'modify' to modify a credit card\nEnter 'back' to return or 'logout' to logout");
                Console.WriteLine("\nInput:");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }

    public static void displayAddress(NpgsqlConnection connection, string ID)
    {
        connection.Open();
        using NpgsqlCommand cmd = new NpgsqlCommand("select address_id, shipping_address FROM Shipping_addresses WHERE customer_id  = " + ID+ " ORDER BY address_id;", connection);

        using NpgsqlDataReader reader = cmd.ExecuteReader();
        Console.WriteLine("ID\tAddress");
        Console.WriteLine("---------------------------------------------------------------------------------");
        while (reader.Read())
        {
            Console.WriteLine(reader[0]+"\t"+ reader[1]);
        }
        connection.Close();
    }
    public static void addAddress(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nAdd a new Address");
        Console.WriteLine("Enter new Address\nAlternativley enter 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back")
            {
                products(connection, ID);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                //Get next valid address ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("select address_id FROM Shipping_addresses WHERE customer_id  = " + ID + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                int i = 1;
                while (reader.Read())
                {
                    i++;
                }
                connection.Close();

                //Add new address
                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("INSERT INTO Shipping_addresses (address_id, customer_id, shipping_address) VALUES (" + i.ToString() + ", " + ID + ", '" + input + "');", connection);
                int rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();

                addresses(connection, ID);
                break;
            }
        }
    }
    public static void modifyAddress(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nModify an Address");
        Console.WriteLine("Enter an address ID to modify\nAlternativley enter 'view' to view current addresses or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (int.TryParse(input, out temp))
            {
                //Check if valid ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("select address_id FROM Shipping_addresses WHERE customer_id  = " + ID + " AND address_id = " + input + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Unknown address ID");
                    connection.Close();
                    continue;
                }
                connection.Close();

                Console.WriteLine("Enter updated address:");
                string newAddress = Console.ReadLine();

                //Modify address
                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("Update shipping_addresses SET shipping_address = '" + newAddress + "' where customer_id  = " + ID + " AND address_id = " + input + ";", connection);
                int rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
                Console.WriteLine("Address successfully updated");
                Console.ReadLine();

                addresses(connection, ID);
                break;
            }
            else if (input == "back")
            {
                addresses(connection, ID);
                break;
            }
            else if (input == "view")
            {
                displayAddress(connection, ID);
                Console.WriteLine("Enter an address ID to modify\nAlternativley enter 'view' to view current addresses or 'back' to return");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter address ID as an int. Please Try Again:");
            }
        }
    }
    public static void removeAddress(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nRemove an Address");
        Console.WriteLine("Enter an address ID to delete\nAlternativley enter 'view' to view current addresses or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (int.TryParse(input, out temp))
            {
                //Check if valid ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("select address_id FROM Shipping_addresses WHERE customer_id  = " + ID + " AND address_id = "+input+";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Unknown address ID");
                    connection.Close();
                    continue;
                }
                connection.Close();

                //Delete address at that ID
                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("DELETE FROM shipping_addresses WHERE address_id = " + input + " AND customer_id = "+ID+";", connection);
                int rowsAffected = cmd1.ExecuteNonQuery();
                connection.Close();
                Console.WriteLine("Address successfully deleted");
                Console.ReadLine();

                addresses(connection, ID);
                break;
            }
            else if (input == "back")
            {
                addresses(connection, ID);
                break;
            }
            else if (input == "view")
            {
                displayAddress(connection, ID);
                Console.WriteLine("Enter an an address ID to delete\nAlternativley enter 'view' to view current addresses or 'back' to return");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter address ID as an int. Please Try Again:");
            }
        }
    }

    public static void addresses(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("View and edit addresses");
        Console.WriteLine("\nEnter 'view' to view list of address\nEnter 'add' to add a new adderess\nEnter 'delete' to delete an address\nEnter 'modify' to modify an address\nEnter 'back' to return or 'logout' to logout");
        Console.WriteLine("\nInput:");
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back")
            {
                user(connection, ID);
                break;
            }
            else if (input == "logout")
            {
                loginUser(connection);
                break;
            }
            else if (input == "add")
            {
                addAddress(connection, ID); 
                break;
            }
            else if (input == "remove" || input == "delete")
            {
                removeAddress(connection, ID);
                break;
            }
            else if (input == "modify")
            {
                modifyAddress(connection, ID);
                break;
            }
            else if (input == "view")
            {
                displayAddress(connection, ID);
                Console.WriteLine("\nEnter 'view' to view list of address\nEnter 'add' to add a new adderess\nEnter 'delete' to delete an address\nEnter 'modify' to modify an address\nEnter 'back' to return or 'logout' to logout");
                Console.WriteLine("\nInput:");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }


    //User home page
    private static void user(NpgsqlConnection connection, string ID)
    {
        Console.Clear();

        //Get user name
        connection.Open();
        using NpgsqlCommand cmd = new NpgsqlCommand("SELECT name FROM Customer WHERE customer_id = "+ID+";", connection);
        using NpgsqlDataReader reader = cmd.ExecuteReader();
        reader.Read();
        string name = reader.GetString(0);
        connection.Close();

        Console.WriteLine("Welcome "+name+"!");
        Console.WriteLine("\nEnter 'shop' to start an order\nEnter 'address' to view and edit addresses\nEnter 'credit cards' to view and edit credit cards\nEnter 'balance' to view current account balance\nEnter 'logout' to logout or 'quit' to exit the application");
        Console.WriteLine("\nInput:");
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back" || input == "logout")
            {
                loginUser(connection);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if(input == "shop" || input == "order")
            {
                shop(connection, ID);
                break;
            }
            else if (input == "addresses" || input == "address")
            {
                addresses(connection, ID);
                break;
            }
            else if (input == "credit cards" || input == "credit card")
            {
                creditCards(connection, ID);
                break;
            }
            else if (input == "balance")
            {
                //Display user balance
                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("SELECT balance FROM Customer WHERE customer_id = " + ID + ";", connection);
                using NpgsqlDataReader reader1 = cmd1.ExecuteReader();
                reader1.Read();

                Console.WriteLine("Current Balance: $" + reader1[0]);
                Console.WriteLine("\nEnter a new command:");
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }

    //Display products for staff purposes
    private static void viewProducts(NpgsqlConnection connection)
    {
        //Display products
        connection.Open();
        using NpgsqlCommand cmd = new NpgsqlCommand("select product_id, name, price, category, brand, size, description FROM product ORDER BY product_id", connection);

        using NpgsqlDataReader reader = cmd.ExecuteReader();
        Console.WriteLine("{0,-5} {1,-20} {2,-8} {3,-15} {4, -20} {5, -8} {6, -60}", "ID", "Name", "Price", "Category", "Brand", "Size", "Description");
        Console.WriteLine("-----------------------------------------------------------------------------------------------------------------------");
        while (reader.Read())
        {
            Console.WriteLine("{0,-5} {1,-20} {2,-8} {3,-15} {4, -20} {5, -8} {6, -60}", reader[0], reader[1], reader[2], reader[3], reader[4], reader[5], reader[6]);
        }
        connection.Close();
    }

    //Delete product
    private static void deleteProduct(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nDelete a Product");
        Console.WriteLine("Enter a product ID to delete\nAlternativley enter 'view' to view products or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (int.TryParse(input, out temp))
            {
                //Check if valid ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("SELECT product_id FROM product where product_id = " + input + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Unknown product ID");
                    connection.Close();
                    continue;
                }
                connection.Close();

                //Delete product at that ID
                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("DELETE FROM product WHERE product_id = " + input + ";", connection);
                int rowsAffected = cmd1.ExecuteNonQuery() ;
                connection.Close();
                Console.WriteLine("Product successfully deleted");
                Console.ReadLine();

                products(connection, ID);
                break;
            }
            else if (input == "back")
            {
                products(connection, ID);
                break;
            }
            else if (input == "view")
            {
                viewProducts(connection);
                Console.WriteLine("\nEnter a product ID to delete\nAlternativley enter 'view' to view products or 'back' to return");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter product ID as an int Please Try Again:");
            }
        }
    }

    //Modify Product
    private static void modifyProduct(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nModify Product");
        Console.WriteLine("Enter a product ID to modify\nAlternativley enter 'view' to view products or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();
            if (int.TryParse(input, out temp))
            {
                //Check for valid ID
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("SELECT product_id FROM product where product_id = " + input + ";", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();
                reader.Read();
                if (!reader.HasRows)
                {
                    Console.WriteLine("Unknown product ID");
                    connection.Close();
                    continue;
                }
                connection.Close();

                //Choose field to update
                string product_id = input;
                Console.WriteLine("\nEnter what field you want to update (Price, Name, Category, Size, Brand, Description)\nOr enter back to return");
                Console.WriteLine("\nInput:");
                while (input != "back")
                {
                    input = Console.ReadLine();

                    if(input == "close" || input == "back")
                    {
                        input = "back";
                    }
                    else if(input == "price")
                    {
                        Console.WriteLine("Enter new product price:");
                        string product_price = Console.ReadLine();
                        double price;
                        while (!double.TryParse(product_price, out price))
                        {
                            Console.WriteLine("Invalid Price. Try again");
                            product_price = Console.ReadLine();
                        }

                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update product SET price = " + price + "' where product_id = " + product_id + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("\nEnter new field to update or enter 'back' to return");
                    }
                    else if (input == "name")
                    {
                        Console.WriteLine("Enter new Name:");
                        string name = Console.ReadLine();

                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update product SET name = '" + name + "' where product_id = " + product_id + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("\nEnter new field to update or enter 'back' to return");
                    }
                    else if (input == "category")
                    {
                        Console.WriteLine("Enter new Category:");
                        string category = Console.ReadLine();

                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update product SET category = '" + category + "' where product_id = " + product_id + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("\nEnter new field to update or enter 'back' to return");
                    }
                    else if (input == "size")
                    {
                        Console.WriteLine("Enter new product size:");
                        string product_size = Console.ReadLine();
                        while (!int.TryParse(product_size, out temp))
                        {
                            Console.WriteLine("Invalid size. Enter size as and int");
                            product_size = Console.ReadLine();
                        }

                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update product SET size = " + product_size + " where product_id = " + product_id + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("\nEnter new field to update or enter 'back' to return");
                    }
                    else if (input == "brand")
                    {
                        Console.WriteLine("Enter new Description:");
                        string brand = Console.ReadLine();

                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update product SET brand = '" + brand + "' where product_id = " + product_id + ";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("\nEnter new field to update or enter 'back' to return");
                    }
                    else if (input == "description")
                    {
                        Console.WriteLine("Enter new Description:");
                        string description = Console.ReadLine();
                        
                        connection.Open();
                        using NpgsqlCommand cmd1 = new NpgsqlCommand("Update product SET description = '"+description+"' where product_id = "+product_id+";", connection);
                        int rowsAffected = cmd1.ExecuteNonQuery();
                        connection.Close();

                        Console.WriteLine("\nEnter new field to update or enter 'back' to return");
                    }
                    else
                    {
                        Console.WriteLine("Not a valid field to update. Try again:");
                    }

                }
                Console.WriteLine("Enter a product ID to modify\nAlternativley enter 'view' to view products or 'back' to return");
                Console.WriteLine("\nInput:");
            }
            else if (input == "back")
            {
                products(connection, ID);
                break;
            }
            else if (input == "view")
            {
                viewProducts(connection);
                Console.WriteLine("\nEnter a product ID to modify\nAlternativley enter 'view' to view products or 'back' to return");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter product ID as an int. Please Try Again:");
            }
        }
    }

    //Add product page
    private static void addProduct(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("\nAdd a new Product");
        Console.WriteLine("Enter new product ID\nAlternativley enter 'view' to view products or 'back' to return");
        Console.WriteLine("\nInput:");

        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if(int.TryParse(input, out temp))
            {
                string product_id = input;
                Console.WriteLine("Enter new product name:");
                string product_name = Console.ReadLine();

                Console.WriteLine("Enter product price:");
                string product_price = Console.ReadLine();
                double price;
                while (!double.TryParse(product_price, out price)){
                    Console.WriteLine("Invalid Price. Try again");
                    product_price = Console.ReadLine();
                }

                Console.WriteLine("Enter product brand:");
                string product_brand = Console.ReadLine();

                Console.WriteLine("Enter product category:");
                string product_category = Console.ReadLine();

                Console.WriteLine("Enter product size:");
                string product_size = Console.ReadLine();
                while (!int.TryParse(product_size, out temp))
                {
                    Console.WriteLine("Invalid size. Enter size as and int");
                    product_size = Console.ReadLine();
                }

                Console.WriteLine("Enter product Description:");
                string description = Console.ReadLine();

                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO product (product_id, name, price, category, brand, size, description) VALUES (" + product_id + ", '" + product_name + "', " + product_price + ", '" + product_category + "', '" + product_brand + "', " + product_size + ", '" + description + "');", connection);
                int rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();

                products(connection, ID);
                break;
            }
            else if (input == "back")
            {
                products(connection, ID);
                break;
            }
            else if (input == "view")
            {
                viewProducts(connection);
                Console.WriteLine("\nEnter new product ID\nAlternativley enter 'view' to view products or 'back' to return");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command / Enter new ID as an int. Please Try Again:");
            }
        }
    }

    //Products CRUD page
    private static void products(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("Manage Products");
        Console.WriteLine("\nEnter 'view' to view product list\nEnter 'add' to add a new product\nEnter 'delete' to delete a product\nEnter 'modify' to modify a product\nEnter 'back' to return or 'logout' to logout");
        Console.WriteLine("\nInput:");
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back")
            {
                staff(connection, ID);
                break;
            }
            else if (input == "logout")
            {
                loginStaff(connection);
                break;
            }
            else if (input == "add")
            {
                addProduct(connection, ID);
                break;
            }
            else if (input == "remove" || input == "delete")
            {
                deleteProduct(connection, ID);
                break;
            }
            else if (input == "modify")
            {
                modifyProduct(connection, ID);
                break;
            }
            else if (input == "view")
            {
                viewProducts(connection);
                Console.WriteLine("\nEnter 'view' to view product list\nEnter 'add' to add a new product\nEnter 'delete' to delete a product\nEnter 'modify' to modify a product\nEnter 'back' to return or 'logout' to logout");
                Console.WriteLine("\nInput:");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }

    //Add stock page
    private static void stock(NpgsqlConnection connection, string ID)
    {
        Console.Clear();
        Console.WriteLine("Add stock and manage warehouses");
        Console.WriteLine("\nEnter 'view warehouse' to view warehouses and availible space\nEnter 'view products' to view list of products\nEnter 'add' or 'add stock'\nEnter 'back' to return or 'logout' to logout");
        Console.WriteLine("\nInput:");
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "back")
            {
                staff(connection, ID);
                break;
            }
            else if (input == "logout")
            {
                loginStaff(connection);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if(input == "view warehouse")
            {
                //Get availble warehouse size
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("with itemVolume as (select size * quantity as volume, warehouse_id "+
                    "from product inner join stock on product.product_id = stock.product_id), "+
                    "spaceUsed as (Select itemVolume.warehouse_id, Sum(volume) as totalVolume "+
                    "from itemVolume inner join warehouse on warehouse.warehouse_id = itemVolume.warehouse_id "+
                    "group by itemVolume.warehouse_id)select warehouse.warehouse_id, size, size - totalVolume as availibleSpace "+
                    "from spaceUsed full outer join warehouse on spaceUsed.warehouse_id = warehouse.warehouse_id;", connection);

                using NpgsqlDataReader reader = cmd.ExecuteReader();
                Console.WriteLine("ID\tSize\tAvailible Space");
                Console.WriteLine("--------------------------------");
                while (reader.Read())
                {
                    Console.WriteLine(reader[0] + "\t" + reader[1] + "\t" + reader[2]);
                }
                connection.Close();
                Console.WriteLine("\nEnter 'view warehouse' to view warehouses and availible space\nEnter 'view products' to view list of products\nEnter 'add' or 'add stock'\nEnter 'back' to return or 'logout' to logout");
                Console.WriteLine("\nInput:");
            }
            else if(input == "view products")
            {
                viewProducts(connection);
                Console.WriteLine("\nEnter 'view warehouse' to view warehouses and availible space\nEnter 'view products' to view list of products\nEnter 'add' or 'add stock'\nEnter 'back' to return or 'logout' to logout");
                Console.WriteLine("\nInput:");
            }
            else if(input == "add" || input == "add stock")
            {
                string warehouse_id = "";
                string product_id = "";
                string quantity = "";
                bool validInput = false;

                //Check if ID exists
                Console.WriteLine("Enter warehouse ID:");
                while (true)
                {
                    warehouse_id = Console.ReadLine();
                    if (int.TryParse(warehouse_id, out temp))
                    {
                        connection.Open();
                        using NpgsqlCommand cmd3 = new NpgsqlCommand("SELECT warehouse_id FROM warehouse where warehouse_id = " + warehouse_id + ";", connection);
                        using NpgsqlDataReader reader1 = cmd3.ExecuteReader();
                        reader1.Read();

                        if (reader1.HasRows)
                        {
                            connection.Close();
                            break;
                        }
                        connection.Close();
                    }
                    Console.WriteLine("Unkown warehouse ID. Try again:");
                }
                Console.WriteLine("Enter product ID");
                while (true)
                {
                    product_id = Console.ReadLine();
                    if (int.TryParse(product_id, out temp))
                    {
                        connection.Open();
                        using NpgsqlCommand cmd2 = new NpgsqlCommand("SELECT product_id FROM product where product_id = " + product_id + ";", connection);
                        using NpgsqlDataReader reader2 = cmd2.ExecuteReader();
                        reader2.Read();

                        if (reader2.HasRows)
                        {
                            connection.Close();
                            break;
                        }
                        connection.Close();
                    }
                    Console.WriteLine("Unkown product ID. Try again:");
                }

                connection.Open();
                using NpgsqlCommand cmd1 = new NpgsqlCommand("with itemVolume as (select size * quantity as volume, warehouse_id " +
                    "from product inner join stock on product.product_id = stock.product_id), " +
                    "spaceUsed as (Select itemVolume.warehouse_id, Sum(volume) as totalVolume " +
                    "from itemVolume inner join warehouse on warehouse.warehouse_id = itemVolume.warehouse_id " +
                    "group by itemVolume.warehouse_id) select warehouse.warehouse_id, size, size - totalVolume as availibleSpace " +
                    "from spaceUsed full outer join warehouse on spaceUsed.warehouse_id = warehouse.warehouse_id WHERE warehouse.warehouse_id = "+warehouse_id+ ";", connection);

                using NpgsqlDataReader reader = cmd1.ExecuteReader();
                reader.Read();
                int availibleSpace = 5000;
                if(!reader.IsDBNull(2))
                {
                    availibleSpace = reader.GetInt32(2);
                }
                connection.Close();

                Console.WriteLine("Enter quantity");
                quantity = Console.ReadLine();
                while(!int.TryParse(quantity, out temp))
                {
                    Console.WriteLine("Not an integer, try again");
                    quantity = Console.ReadLine();
                }
                while(temp > availibleSpace)
                {
                    Console.WriteLine("Not enough space in warehouse, try again");
                    quantity = Console.ReadLine();
                    while (!int.TryParse(quantity, out temp))
                    {
                        Console.WriteLine("Not an integer, try again");
                        quantity = Console.ReadLine();
                    }
                }


                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO stock (product_id, warehouse_id, quantity) VALUES (" + product_id + ", " + warehouse_id + ", " + quantity+" );", connection);
                int rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();

                Console.WriteLine("\nEnter 'view warehouse' to view warehouses and availible space\nEnter 'view products' to view list of products\nEnter 'add' or 'add stock'\nEnter 'back' to return or 'logout' to logout");
                Console.WriteLine("\nInput:");
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }

    //Staff home page
    private static void staff(NpgsqlConnection connection, string ID)
    {
        Console.Clear();

        //Get staff name
        connection.Open();
        using NpgsqlCommand cmd = new NpgsqlCommand("SELECT name FROM Staff WHERE staff_id = " + ID + ";", connection);
        using NpgsqlDataReader reader = cmd.ExecuteReader();
        reader.Read();
        string name = reader.GetString(0);
        connection.Close();

        Console.WriteLine("Welcome " + name + "!");
        Console.WriteLine("\nEnter 'logout' to logout of this account.\nEnter 'products' to add, modify, or delete products.\nEnter 'stock' to add stock to warehouses");
        Console.WriteLine("\nInput:");
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if(input == "back" ||  input == "logout")
            {
                loginStaff(connection);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if (input == "products")
            {
                products(connection, ID);
                break;
            }
            else if (input == "stock")
            {
                stock(connection, ID); 
                break;
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }

    //Create new user
    private static void newUser(NpgsqlConnection connection)
    {
        Console.Clear();
        Console.WriteLine("Welcome to the New User page\n");
        Console.WriteLine("Enter New User ID OR enter 'back' to return to the login page:");

        //Take input
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            //Check if input is numeric
            if (int.TryParse(input, out temp))
            {       
                //input user name
                Console.WriteLine("Enter your Name:");
                string name = Console.ReadLine();

                //insert user name into table
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO Customer (customer_id, name, balance) VALUES ("+input+", '"+name+"', 0.00);", connection);
                int rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
                //return to login page
                loginUser(connection);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if(input == "back")
            {
                loginUser(connection);
                break;
            }
            else
            {
                Console.WriteLine("Unkown command. Format new user id as an int. Please Try Again");
            }
        }     
    }

    //Create new staff
    private static void newStaff(NpgsqlConnection connection)
    {
        Console.Clear();
        Console.WriteLine("Welcome New Staff Member!\n");
        Console.WriteLine("Enter New Staff ID OR enter 'back' to return to the login page:");

        //Take input
        string input = "";
        int temp;

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            //Check if input is numeric
            if (int.TryParse(input, out temp))
            {
                //input user name
                Console.WriteLine("Enter Your Name:");
                string name = Console.ReadLine();

                //insert user name into table
                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("INSERT INTO Staff (staff_id, name, salary) VALUES (" + input + ", '" + name + "', 9999.99);", connection);
                int rowsAffected = cmd.ExecuteNonQuery();
                connection.Close();
                //return to login page
                loginStaff(connection);
                break;
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if (input == "back")
            {
                loginStaff(connection);
                break;
            }
            else
            {
                Console.WriteLine("Unkown command. Format new user id as an int. Please Try Again");
            }
        }
    }

    //User login page
    private static void loginUser(NpgsqlConnection connection)
    {
        Console.Clear();
        Console.WriteLine("Welcome to the User login page\n");
        Console.WriteLine("Enter User ID to login\nIf your are a new user, type 'new' or 'new user' to create a new account\nOR enter 'back' to return to main page");

        //Take input
        Console.WriteLine("\nInput:");
        string input = "";

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();
            int temp;

            //Check if input is numeric
            if (int.TryParse(input, out temp))
            {
                //Check for matching ID
                bool success = false;

                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("SELECT customer_id FROM Customer;", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();

                int val;
                while (reader.Read())
                {
                    val = reader.GetInt32(0);
                    //Match found
                    if (val == temp)
                    {
                        success = true;
                        break;
                    }
                }
                connection.Close();

                //Login if success
                if (success) {
                    user(connection, input);
                    break;
                }
                else Console.WriteLine("Unknown User ID. Please Try Again");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if(input == "back")
            {
                login(connection);
                break;
            }
            else if(input == "new" || input == "n" || input == "new user")
            {
                newUser(connection);
                break;
            }
            else
            {
                Console.WriteLine("Unkown command / User ID. Please Try Again");
            }
        }
    }

    //Staff login page
    private static void loginStaff(NpgsqlConnection connection)
    {
        Console.Clear();
        Console.WriteLine("Welcome to the Staff login page\n");
        Console.WriteLine("Enter Staff ID to login\nIf your are a new staff member, type 'new' or 'new staff' to create a new account\nOR enter 'back' to return to main page");

        //Take input
        Console.WriteLine("\nInput:");
        string input = "";

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();
            int temp;

            //Check if input is numeric
            if (int.TryParse(input, out temp))
            {
                //Check if ID exists
                bool success = false;

                connection.Open();
                using NpgsqlCommand cmd = new NpgsqlCommand("SELECT staff_id FROM Staff;", connection);
                using NpgsqlDataReader reader = cmd.ExecuteReader();

                int val;
                while (reader.Read())
                {
                    val = reader.GetInt32(0);
                    //Check match
                    if (val == temp)
                    {
                        success = true;
                        break;
                    }
                }
                connection.Close();

                //Login if successful
                if (success)
                {
                    staff(connection, input);
                    break;
                }
                else Console.WriteLine("Unknown Staff ID. Please Try Again");
            }
            else if (input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else if (input == "back")
            {
                login(connection);
                break;
            }
            else if (input == "new" || input == "n" || input == "new staff")
            {
                newStaff(connection);
                break;
            }
            else
            {
                Console.WriteLine("Unkown command / User ID. Please Try Again");
            }
        }
    }

    // Login page
    private static void login(NpgsqlConnection connection)
    {
        Console.Clear();
        Console.WriteLine("Welcome to Group 19's Shopping Applicaiton!\n");
        Console.WriteLine("Type 'user' to login as a user,\nType 'staff' to login as a staff,\nAlternativley, type 'close' to exit the application");

        //Take input
        Console.WriteLine("\nInput:");
        string input = "";

        while (input != "close")
        {
            input = Console.ReadLine();
            input = input.ToLower();

            if (input == "staff")
            {
                loginStaff(connection);
                break;
            }
            else if(input == "user")
            {
                loginUser(connection);
                break;
            }
            else if(input == "quit" || input == "q" || input == "c")
            {
                input = "close";
            }
            else
            {
                Console.WriteLine("Unkown command. Please Try Again:");
            }
        }
    }

    //program main method
    public static int Main(string[] args)
    {
        var connectionString = "Host=localhost;" +
            "Username=postgres;" +
            "Password=Beans;" +     //replace "(your-passowrd)" with your postgres user password
            "Database=DDD-project;";
        using NpgsqlConnection connection = new NpgsqlConnection(connectionString);

        login(connection);
        
        return 0;
    }
}
